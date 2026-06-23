using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using MyAjaxApi.Data;
using MyAjaxApi.Infrastructure;

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
builder.Services.AddSwaggerGen();

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
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 開發環境才開放 Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 全域例外處理（放最前面，才能攔截後續 Middleware 的錯誤）
app.UseExceptionHandler();

// CORS 必須放在授權中介軟體之前，
// 否則 Preflight（OPTIONS）請求可能被擋掉
app.UseCors("AllowFrontend");

// 提供 wwwroot 內的靜態檔案（HTML / JS / CSS）
app.UseDefaultFiles();   // / → /index.html
app.UseStaticFiles();

// UseAuthorization：授權中介軟體。本範例沒有需要登入的端點，
//   保留為 .NET 範本慣例；登入與 [Authorize] 保護見另一門 JWT 課程。
app.UseAuthorization();

app.MapControllers();

app.Run();
