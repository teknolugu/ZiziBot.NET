using FluentValidation;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.Zizi.Models.Validators;

public class SpamWatchConfigValidator : AbstractValidator<SpamWatchConfig>
{
    public SpamWatchConfigValidator()
    {
        RuleFor(config => config.BaseUrl).NotEmpty().NotEmpty();
        RuleFor(config => config.ApiToken).NotEmpty().NotNull();
    }
}