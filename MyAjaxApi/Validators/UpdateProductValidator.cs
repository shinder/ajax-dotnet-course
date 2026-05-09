using FluentValidation;
using MyAjaxApi.Models;

namespace MyAjaxApi.Validators;

// 更新商品的驗證規則，與 CreateProductValidator 規則相同。
// 實務上若規則一致可以共用一個基底類別，這裡為了清楚分開示範。
public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名稱不可為空")
            .MaximumLength(100).WithMessage("名稱不超過 100 字");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("價格必須大於 0")
            .LessThanOrEqualTo(1_000_000).WithMessage("價格不超過 1,000,000");

        RuleFor(x => x.ImageUrl)
            .Must(url => url!.StartsWith("https://"))
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("圖片 URL 必須使用 https");
    }
}
