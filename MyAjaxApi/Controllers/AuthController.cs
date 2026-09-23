using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAjaxApi.Data;
using MyAjaxApi.Models;
using MyAjaxApi.Services;

namespace MyAjaxApi.Controllers;

// JWT 登入流程的三個端點：
//   POST /api/auth/register  註冊（密碼以 bcrypt 雜湊後存入 DB）
//   POST /api/auth/login     登入（比對密碼，成功就簽發 token）
//   GET  /api/auth/me        需帶 token，回傳目前登入者（用來確認 token 有效）
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<MeDto>> Register(RegisterDto dto)
    {
        var username = dto.Username.Trim();

        // 先查一次帳號是否重複，給使用者友善的 409 訊息
        if (await _db.Users.AnyAsync(u => u.Username == username))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "帳號已被使用");

        var user = new User
        {
            Username = username,
            // HashPassword 會自動產生隨機 salt 並和 work factor 一起編進結果字串，
            // 所以同一組密碼每次雜湊出來的字串都不同，也不需要另外存 salt
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 兩個請求同時註冊同一個帳號時，上面的 AnyAsync 可能都通過，
            // 最後靠資料庫的唯一索引擋下第二筆；這裡把它轉成同樣的 409
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "帳號已被使用");
        }

        // 註冊成功回 201。不直接回 token，讓「註冊」和「登入」兩個動作分開，流程比較清楚
        return CreatedAtAction(nameof(Me), null, new MeDto { Id = user.Id, Username = user.Username });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == dto.Username.Trim());

        // 帳號不存在與密碼錯誤「一律」回同一句話，
        // 不要讓攻擊者能透過訊息差異判斷哪些帳號存在（帳號列舉攻擊）。
        // Verify 會從雜湊字串取出 salt 與 work factor，用同樣的參數重算後比對。
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "帳號或密碼錯誤");

        var (token, expiresAt) = _tokenService.CreateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = user.Username
        });
    }

    // GET /api/auth/me
    // [Authorize]：沒有帶 token、token 過期或簽章不符，JwtBearer 中介軟體會直接回 401，
    // 根本不會進到這個方法。能進來就代表 token 有效。
    [HttpGet("me")]
    [Authorize]
    public ActionResult<MeDto> Me()
    {
        // User 是 ControllerBase 的屬性（ClaimsPrincipal），
        // JwtBearer 驗證成功後會把 token 的 Payload 還原成 Claims 放在這裡。
        // 這裡讀的 "sub"、"name" 就是 TokenService.CreateToken 放進去的
        // （Program.cs 設定 MapInboundClaims = false，claim 名稱才不會被改成 .NET 的長名稱）。
        var id = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var username = User.FindFirstValue(JwtRegisteredClaimNames.Name)!;

        return Ok(new MeDto { Id = id, Username = username });
    }
}
