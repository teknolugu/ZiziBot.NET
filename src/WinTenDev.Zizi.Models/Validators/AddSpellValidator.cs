using FluentValidation;
using WinTenDev.Zizi.Models.Tables;

namespace WinTenDev.Zizi.Models.Validators;

public class AddSpellValidator : AbstractValidator<Spell>
{
    public AddSpellValidator()
    {
        RuleFor(spell => spell.Typo).NotEmpty().WithMessage("Typo is required");
        RuleFor(spell => spell.Fix).NotEmpty().WithMessage("Typo is required");
    }
}
