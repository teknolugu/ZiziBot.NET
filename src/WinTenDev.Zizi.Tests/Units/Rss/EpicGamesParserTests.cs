using Xunit;

namespace WinTenDev.Zizi.Tests.Units.Rss;

public class EpicGamesParserTests
{
    private readonly EpicGamesService _epicGamesService;

    public EpicGamesParserTests(EpicGamesService epicGamesService)
    {
        _epicGamesService = epicGamesService;
    }

    [Theory]
    [InlineData("supraland")]
    public void GetDetail(string slug)
    {

        Assert.True(true);
    }
}