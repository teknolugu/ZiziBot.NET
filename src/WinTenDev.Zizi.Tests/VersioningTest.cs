using FluentAssertions;
using WinTenDev.Zizi.Utils;
using Xunit;

namespace WinTenDev.Zizi.Tests;

public class VersioningTest
{
    [Fact]
    public void VersioningBuildTest()
    {
        var buildNumber = VersionUtil.GetBuildNumber();
        buildNumber.Should().BeGreaterThan(0);

    }

    [Fact]
    public void RevNumberTest()
    {
        var revNumber = VersionUtil.GetRevNumber();

        revNumber.Should().BeGreaterThan(0);
    }
}