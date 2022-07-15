using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace WinTenDev.Zizi.Services.Externals;

public class AllDebridService
{
    private readonly string BaseUrl = "https://api.alldebrid.com/v4/";

    public async Task<AllDebrid> ConvertUrl(
        string url,
        Action<string> action = null
    )
    {
        // var agent = allDebrid.Agent;
        // var apiKey = "allDebrid.ApiKey";
        var agent = "allDebrid.Agent";
        var apiKey = "allDebrid.ApiKey";

        action("Anuu");

        var urlApi = Url.Combine(BaseUrl, "link/unlock");
        var req = await urlApi
            .SetQueryParam("agent", agent)
            .SetQueryParam("apikey", apiKey)
            .SetQueryParam("link", url)
            .GetJsonAsync<AllDebrid>()
            .ConfigureAwait(false);

        return req;
        // var urlResult = req.Status == "success" ? req.DebridData.Link.AbsoluteUri : url;
        // Log.Debug("Debrid result: {0}", urlResult);

        // return urlResult;
    }
}