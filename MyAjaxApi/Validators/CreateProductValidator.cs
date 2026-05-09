using FluentValidation;
using MyAjaxApi.Models;

namespace MyAjaxApi.Validators;

// FluentValidation 比 Data Annotations 更靈活，
// 適合處理跨欄位、條件式等較複雜的驗證規則。
//
// Program.cs 已登記 AddValidatorsFromAssemblyContaining<Program>()，
// 會自動掃描並注入所有繼承 AbstractValidator<T> 的類別。
// AddFluentValidationAutoValidation() 則會在 Model Binding 時自動執行驗證。
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名稱不可為空")
            .MaximumLength(100).WithMessage("名稱不超過 100 字");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("價格必須大於 0")
            .LessThanOrEqualTo(1_000_000).WithMessage("價格不超過 1,000,000");

        // 條件驗證：當 ImageUrl 有值時，必須以 https:// 開頭
        // .When() 後面的條件成立時才執行 .Must()
        RuleFor(x => x.ImageUrl)
            .Must(url => url!.StartsWith("https://"))
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("圖片 URL 必須使用 https");
    }
}
