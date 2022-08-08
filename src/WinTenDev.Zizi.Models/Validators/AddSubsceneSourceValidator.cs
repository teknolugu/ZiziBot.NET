using FluentValidation;
using WinTenDev.Zizi.Models.Entities.MongoDb;

namespace WinTenDev.Zizi.Models.Validators;

public class AddSubsceneSourceValidator : AbstractValidator<SubsceneSource>
{
    public AddSubsceneSourceValidator()
    {
        RuleFor(url => url.SearchTitleUrl).NotEmpty().WithMessage("Search Title Url is required");
        RuleFor(url => url.SearchSubtitleUrl).NotEmpty().WithMessage("Search Subtitle Url is required");
    }
}