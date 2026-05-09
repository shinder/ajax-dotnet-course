namespace MyAjaxApi.Models;

// 登入請求的 DTO：前端 POST /api/auth/login 時送這個格式
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
