using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MyAjaxApi.Models;

namespace MyAjaxApi.Services;

// 對應 appsettings.json 的 "Jwt" 區段。
// Key 不放在 appsettings.json（會進 git），而是用 dotnet user-secrets 設定，
// 啟動時由 Program.cs 檢查是否存在。
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;      // 簽發者，通常填 API 的網址或名稱
    public string Audience { get; set; } = string.Empty;    // 接收者，通常填前端的網址或名稱
    public string Key { get; set; } = string.Empty;         // 簽章用的對稱金鑰，HMAC-SHA256 至少 32 bytes
    public int ExpireMinutes { get; set; } = 60;            // token 有效時間
}

// 把「產生 JWT」集中在一個地方，Controller 只要呼叫 CreateToken(user) 就好。
// JWT 由三段組成：Header.Payload.Signature，用 . 連接後 Base64Url 編碼。
//   Header    ：演算法（HS256）與型別（JWT）
//   Payload   ：claims，也就是「這個 token 代表誰、什麼時候到期」等資訊。注意 Payload 只是編碼不是加密，
//               任何人都能解開來看，所以不要放密碼等敏感資料。
//   Signature ：用 Key 對前兩段做 HMAC，伺服器驗證時重算一次比對，確保內容沒被竄改。
public class TokenService
{
    private readonly JwtSettings _settings;

    public TokenService(JwtSettings settings) => _settings = settings;

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        // Claims：放進 Payload 的鍵值對。
        // sub（subject）放使用者 Id、name 放帳號，之後 [Authorize] 的端點可以從 User.Claims 讀回來。
        // jti（JWT ID）給每個 token 一個唯一識別碼，正式專案可用來實作登出黑名單。
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 簽章金鑰：Program.cs 驗證 token 時必須用同一把 Key，否則簽章對不起來
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpireMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        // WriteToken 把 JwtSecurityToken 物件序列化成 "xxxxx.yyyyy.zzzzz" 字串
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
