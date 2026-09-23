using FluentValidation;
using MyAjaxApi.Models;

namespace MyAjaxApi.Validators;

// 登入只檢查「有沒有填」。長度、格式等規則不在這裡重複，
// 否則等於告訴攻擊者「這種格式的帳號一定不存在」，而且改規則時要改兩處。
public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("請輸入帳號");
        RuleFor(x => x.Password).NotEmpty().WithMessage("請輸入密碼");
    }
}
