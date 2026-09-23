using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MyAjaxApi.Data;
using MyAjaxApi.Infrastructure;
using MyAjaxApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ════════════════════════════════════════════════════════════
// 第一區：服務注入（Dependency Injection）
// builder.Services 是 DI 容器，所有要用的服務都在這裡登記。
// 登記後就可以在 Controller 建構式用參數注入的方式取用。
// ════════════════════════════════════════════════════════════

// 啟用 Controller 路由
builder.Services.AddControllers();

// ── Swagger（API 文件 + 測試介面）────────────────────────────
// 開發階段用來瀏覽所有端點並直接發送請求測試
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    // 讓 Swagger UI 右上角出現 Authorize 按鈕，貼上 token 後測試 [Authorize] 的端點。
    // Scheme 設為 http + bearer，Swagger UI 會自動加上 "Bearer " 前綴，只要貼 token 本體。
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "先呼叫 POST /api/auth/login 取得 token，貼到下方欄位（不用加 Bearer 前綴）"
    });
    // 全域套用：所有端點都顯示鎖頭圖示。沒加 [Authorize] 的端點不帶 token 一樣能呼叫。
    opt.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

// ── EF Core + SQLite ─────────────────────────────────────────
// AddDbContext 把 AppDbContext 登記為 Scoped 服務：
// 每個 HTTP 請求會建立一個新的 DbContext 實例，請求結束後釋放。
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// ── FluentValidation ──────────────────────────────────────────
// 自動掃描目前組件內所有繼承 AbstractValidator<T> 的驗證類別，
// 並在 Model Binding 時自動執行驗證，驗證結果寫入 ModelState。
// 由於 DTO 上沒有 Data Annotations，ModelState 只會收到 FluentValidation 的錯誤。
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── CORS（跨來源資源共用）────────────────────────────────────
// 瀏覽器的同源政策會擋住不同 port 的請求，
// 例如前端在 :5500、後端在 :5269，需要明確允許才能通。
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                "http://localhost:5500",     // VS Code Live Server
                "http://127.0.0.1:5500",
                "http://localhost:3000")     // 其他常見開發 port
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── JWT 驗證 ──────────────────────────────────────────────────
// 讀取 appsettings.json 的 Jwt 區段；Key 不進 git，改由 dotnet user-secrets 提供：
//   dotnet user-secrets set "Jwt:Key" "<至少 32 個字元的隨機字串>"
// 沒設定就在啟動時直接失敗，比執行到登入才出錯好找問題。
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("appsettings.json 缺少 Jwt 區段");
if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
    throw new InvalidOperationException(
        "Jwt:Key 未設定或太短（HMAC-SHA256 至少 32 bytes）。請執行：" +
        "dotnet user-secrets set \"Jwt:Key\" \"<至少 32 個字元的隨機字串>\"");
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<TokenService>();

// AddAuthentication：登記「驗證」機制，預設用 JwtBearer。
// JwtBearer 會從每個請求的 Authorization: Bearer <token> 標頭取出 token，
// 依 TokenValidationParameters 檢查簽章、簽發者、接收者、到期時間，
// 通過就把 Payload 還原成 ClaimsPrincipal 放進 HttpContext.User。
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        // 預設會把 "sub" 改名成 ClaimTypes.NameIdentifier 這種長名稱，
        // 關掉之後 Controller 讀到的 claim 名稱就和 TokenService 放進去的一樣
        opt.MapInboundClaims = false;

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,            // 檢查 exp，過期的 token 回 401
            ValidateIssuerSigningKey = true,    // 用 Key 重算簽章比對，防竄改
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            // 預設允許 5 分鐘的時鐘誤差，本範例前後端在同一台機器，設為 0 讓到期時間精準
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name"              // 讓 User.Identity.Name 對應到 "name" claim
        };
    });
builder.Services.AddAuthorization();

// ── 全域例外處理 ──────────────────────────────────────────────
// 攔截所有未捕捉的例外，統一回傳 500 格式，避免洩漏 Stack Trace。
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ════════════════════════════════════════════════════════════
// 第二區：建立應用程式並設定 Middleware Pipeline
// Middleware 依照 Use* 的登記順序執行，順序很重要！
// ════════════════════════════════════════════════════════════

var app = builder.Build();

// 應用程式啟動時自動建立資料庫（開發用）
// 正式專案應改用 dotnet ef database update（Migration）
// 注意：EnsureCreated 只在「資料庫不存在」時建表，之後新增的 DbSet（例如 Users）不會補上。
// 若 app.db 是加入 Users 之前建立的，請先刪掉 app.db 再啟動。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 全域例外處理（放最前面，才能攔截後續所有 Middleware 的錯誤）
app.UseExceptionHandler();

// 開發環境才開放 Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS 必須放在授權中介軟體之前，
// 否則 Preflight（OPTIONS）請求可能被擋掉
app.UseCors("AllowFrontend");

// 提供 wwwroot 內的靜態檔案（HTML / JS / CSS）
app.UseDefaultFiles();   // / → /index.html
app.UseStaticFiles();

// 順序很重要：UseAuthentication 必須在 UseAuthorization 之前。
//   UseAuthentication：解析 Authorization 標頭的 token，建立 HttpContext.User（是誰）
//   UseAuthorization ：依 [Authorize] 決定這個 User 能不能進端點（能不能）
// 順序反了會導致所有 [Authorize] 的端點一律 401，因為授權時 User 還沒建立。
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
