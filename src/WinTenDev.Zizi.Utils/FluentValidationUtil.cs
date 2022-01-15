using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;

// Reference: https://github.com/baldricloth/FluentValidation.Extensions.QuickValidate/blob/master/FluentValidation.Extensions.QuickValidate/ValidationExtension.cs
namespace WinTenDev.Zizi.Utils;

public static class FluentValidationUtil
{
    public static ValidationResult Validate<TValidator, TObj>(this TObj toValidate) where TValidator : class, new() where TObj : class
    {
        var validatorInstance = new TValidator() as AbstractValidator<TObj>;
        var results = validatorInstance?.Validate(toValidate);

        return results;
    }

    public static async Task<ValidationResult> ValidateAsync<TValidator, TObj>(this TObj toValidate) where TValidator : class, new() where TObj : class
    {
        var validatorInstance = new TValidator() as AbstractValidator<TObj>;
        var results = await validatorInstance?.ValidateAsync(toValidate)!;

        return results;
    }
}