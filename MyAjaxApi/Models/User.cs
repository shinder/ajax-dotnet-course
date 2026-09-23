namespace MyAjaxApi.Models;

// Domain Model：對應資料庫 Users 資料表。
// 密碼「絕對不能」以明文儲存，只存 bcrypt 雜湊後的字串；
// 登入時用 BCrypt.Verify 比對，雜湊本身無法還原成原始密碼。
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// 註冊：帳號 + 密碼
public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// 登入：帳號 + 密碼（與註冊欄位相同，但驗證規則不同，所以分開定義）
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// 登入成功的回應：token 本體 + 到期時間 + 帳號。
// 前端把 token 存起來，之後每個請求用 Authorization: Bearer <token> 帶上。
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
}

// GET /api/auth/me 的回應：從 token 的 claims 還原出來的使用者資訊
public class MeDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}
