using FluentValidation;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Models.Validators;

public class TagParamValidator : AbstractValidator<CloudTag>
{
    public TagParamValidator()
    {
        RuleFor(param => param.ChatId).NotEmpty();
        RuleFor(param => param.FromId).NotEmpty();
        RuleFor(param => param.Tag).MinimumLength(3).NotEmpty();
    }
}