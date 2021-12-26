using FluentAssertions;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Models.Validators;
using WinTenDev.Zizi.Utils;
using Xunit;

namespace WinTenDev.Zizi.Tests;

public class FluentValidationUtilTest
{
    [Fact]
    public void ValidateTest()
    {
        var tagData = new CloudTag
        {
            ChatId = 123,
            FromId = 123,
            Tag = "tag"
        };

        var validationResult = tagData.Validate<TagParamValidator, CloudTag>();

        validationResult.Errors.Should().HaveCount(0);
        validationResult.IsValid.Should().BeTrue();
    }
}