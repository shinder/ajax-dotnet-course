using FluentValidation;
using MyAjaxApi.Models;

namespace MyAjaxApi.Validators;

public class UpdateFruitValidator : AbstractValidator<UpdateFruitDto>
{
    public UpdateFruitValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名稱不可為空")
            .MaximumLength(20).WithMessage("名稱不超過 20 字");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("價格必須大於 0")
            .LessThanOrEqualTo(10_000).WithMessage("價格不超過 10,000");
    }
}
