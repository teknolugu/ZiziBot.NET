using System.Threading.Tasks;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace WinTenDev.Zizi.Tests.Units.Rss;

public class FeedReaderTest
{
    public FeedReaderTest(ITestOutputHelper iTestOutputHelper)
    {
        iTestOutputHelper.UseSerilog();
    }

    [Theory]
    [InlineData("https://pahe.li/feed")]
    [InlineData("https://pahe.li/feed/")]
    [InlineData("https://blog.jetbrains.com/feed")]
    [InlineData("https://blog.jetbrains.com/feed/")]
    [InlineData("https://github.com/microsoft/vscode/commits/main")]
    [InlineData("https://github.com/microsoft/vscode/commits/main.atom")]
    [InlineData("https://github.com/microsoft/vscode/commits/main/")]
    [InlineData("https://github.com/microsoft/vscode/commits/main/.atom")]
    [InlineData("https://github.com/microsoft/vscode/releases")]
    [InlineData("https://github.com/microsoft/vscode/releases.atom")]
    public async Task RssParserTest(string url)
    {
        Log.Information("Check URL Feed: {url}", url);

        var tryFix = url.TryFixRssUrl();
        var syndicationFeed = await RssFeedUtil.OpenSyndicationFeed(tryFix);

        Log.Debug("syndicationFeed: {@syndicationFeed}", syndicationFeed);

        Assert.NotNull(syndicationFeed);
    }
}