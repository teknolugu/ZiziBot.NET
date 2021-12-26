using FluentAssertions;
using WinTenDev.Zizi.Utils;
using Xunit;

namespace WinTenDev.Zizi.Tests;

public class UrlUtilTest
{
    [Fact]
    public void IsMegaUrl()
    {
        const string megaUrl = "https://mega.nz/file/Gj5gxxxxxxxxxxxxxxxxxxx";
        const string mediafireUrl = "https://www.mediafire.com/file/gpmxxxxxxxxx";
        const string uptoboxUrl = "https://uptobox.com/zxxxxx";

        megaUrl.IsMegaUrl().Should().BeTrue("It's mega URL");
        mediafireUrl.IsMegaUrl().Should().BeFalse("It's Mediafire URL");
        uptoboxUrl.IsMegaUrl().Should().BeFalse("It's Uptobox URL");
    }
}