using FluentValidation;
using MyAjaxApi.Models;

namespace MyAjaxApi.Validators;

// 水果的驗證規則。驗證失敗時 [ApiController] 會自動回 400，
// Body 是 ValidationProblemDetails，errors 的 key 是屬性名稱（Name、Price），
// 前端可據此把訊息顯示到對應的輸入欄位旁（見 wwwroot/fruits.html 的 showFieldErrors）。
public class CreateFruitValidator : AbstractValidator<CreateFruitDto>
{
    public CreateFruitValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名稱不可為空")
            .MaximumLength(20).WithMessage("名稱不超過 20 字");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("價格必須大於 0")
            .LessThanOrEqualTo(10_000).WithMessage("價格不超過 10,000");
    }
}
