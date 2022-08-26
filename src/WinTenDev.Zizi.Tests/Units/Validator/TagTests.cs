using FluentAssertions;
using Xunit;

namespace WinTenDev.Zizi.Tests.Units.Validator;

public class TagTests
{
    [Fact]
    public void AddTagTest()
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