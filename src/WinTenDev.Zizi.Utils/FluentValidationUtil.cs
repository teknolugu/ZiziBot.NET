using FluentValidation;
using FluentValidation.Results;

namespace WinTenDev.Zizi.Utils;

public static class FluentValidationUtil
{
    public static ValidationResult Validate<TValidator, TObj>(this TObj toValidate) where TValidator : class, new() where TObj : class
    {
        // Reference: https://github.com/baldricloth/FluentValidation.Extensions.QuickValidate/blob/master/FluentValidation.Extensions.QuickValidate/ValidationExtension.cs

        var validatorInstance = new TValidator() as AbstractValidator<TObj>;
        var results = validatorInstance?.Validate(toValidate);

        return results;
    }
}