using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyAjaxApi.Models;

namespace MyAjaxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // IConfiguration 用來讀取 appsettings.json 的設定值
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    // POST /api/auth/login
    // Body: { "username": "admin", "password": "password" }
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        // ⚠️ 示範用途：直接比對明文密碼
        // 實際專案應查資料庫並用 BCrypt 驗證密碼 Hash
        if (dto.Username != "admin" || dto.Password != "password")
            return Unauthorized(new { message = "帳號或密碼錯誤" });   // 401

        var token = GenerateJwt(dto.Username, "Admin");

        // 回傳 Token 給前端，前端存到 localStorage，
        // 之後每次請求都在 Authorization header 帶上這個 Token
        return Ok(new { token });
    }

    // 產生 JWT Token
    private string GenerateJwt(string username, string role)
    {
        var jwtConfig = _config.GetSection("Jwt");

        // 簽名金鑰：用對稱式加密（HMAC-SHA256）
        // 伺服器用同一把 Key 簽發和驗證，Key 絕對不能外洩
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(
            double.Parse(jwtConfig["ExpiresInMinutes"]!));

        // Claims：放在 Token Payload 裡的使用者資訊
        // 這些資訊任何人都能解碼（Base64），但無法偽造（因為有簽章）
        // 所以不要放密碼等敏感資料
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),          // 使用者識別
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Token 唯一 ID
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)   // 角色，供 [Authorize(Roles="Admin")] 使用
        };

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],       // 簽發者
            audience: jwtConfig["Audience"],   // 目標對象
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        // 把 Token 物件序列化成 eyJ... 格式的字串
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
