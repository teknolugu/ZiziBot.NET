using FluentValidation;
using WinTenDev.Zizi.Models.Dto;

namespace WinTenDev.Zizi.Models.Validators;

public class AddSpellDtoValidator : AbstractValidator<SpellDto>
{
    public AddSpellDtoValidator()
    {
        RuleFor(spell => spell.Typo).NotEmpty().WithMessage("Typo is required");
        RuleFor(spell => spell.Fix).NotEmpty().WithMessage("Fix is required");
    }
}