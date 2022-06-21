using Flurl.Http;

namespace WinTenDev.Zizi.Utils;

public static class FlurlUtil
{
    public static IFlurlRequest OpenFlurlSession(this string url)
    {
        return new FlurlRequest(url)
            .WithHeader(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5106.0 Safari/537.36 Edg/104.0.1287.1"
            );
    }
}