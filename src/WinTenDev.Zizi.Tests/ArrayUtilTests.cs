using FluentAssertions;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using Xunit;

namespace WinTenDev.Zizi.Tests;

public class ArrayUtilTests
{
    [Fact]
    public void RandomListTest()
    {
        var randomNames = MemberUtil.GetRandomNames();
        var randomName = randomNames.RandomElement();

        randomName.Should().BeOneOf(randomNames);
    }

    [Fact]
    public void RandomArrayTest()
    {
        var randomNames = MemberUtil.GetRandomNames().ToArray();
        var randomName = randomNames.RandomElement();

        randomName.Should().BeOneOf(randomNames);
    }

    [Fact]
    public void RandomFullName()
    {
        var randomFullName = MemberUtil.GetRandomFullName();

        randomFullName.Should().BeOneOf("fulan bin fulan", "fulanah binti fulan");
    }
}