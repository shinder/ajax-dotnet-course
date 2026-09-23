using FluentValidation;
using MyAjaxApi.Models;

namespace MyAjaxApi.Validators;

// 註冊的驗證規則。驗證失敗時 [ApiController] 會自動回 400，
// errors 的 key 是屬性名稱（Username、Password），前端據此顯示到對應欄位旁。
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("帳號不可為空")
            .Length(3, 20).WithMessage("帳號長度須為 3 到 20 字")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("帳號只能包含英文、數字與底線");

        // bcrypt 只會取密碼的前 72 bytes，超過的部分會被忽略，所以上限設在這之內
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼不可為空")
            .MinimumLength(6).WithMessage("密碼至少 6 個字元")
            .MaximumLength(64).WithMessage("密碼不超過 64 個字元");
    }
}
