using System.Collections.Generic;
using System.Threading.Tasks;
using CacheTower;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types.Pigoora;
using WinTenDev.Zizi.Services.Internals;

namespace WinTenDev.Zizi.Services.Externals;

public class CekResiService
{
    private readonly BinderByteConfig _binderByteConfig;
    private readonly CacheStack _cacheStack;
    private readonly QueryService _queryService;
    private const string PigooraCekResiUrl = "https://api.cekresi.pigoora.com/cekResi";
    private const string CommonUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:85.0) Gecko/20100101 Firefox/85.0";

    public CekResiService(
        IOptionsSnapshot<BinderByteConfig> binderByteConfig,
        CacheStack cacheStack,
        QueryService queryService
    )
    {
        _binderByteConfig = binderByteConfig.Value;
        _cacheStack = cacheStack;
        _queryService = queryService;
    }

    private static List<string> GetCouriers()
    {
        return new List<string> { "sicepat", "jne", "jnt", "ninja", "pos" };
    }

    public async Task<PigooraCekResi> CekResi(string resi)
    {
        return new PigooraCekResi();
    }

    public async Task<string> CekResiCore(string resi)
    {
        Log.Information("Checking Resi {0}", resi);
        var couriers = GetCouriers();

        Log.Debug("Resi Check finish");
        return "filtered";
    }

    public async Task PigooraCekResi(
        string courier,
        string awb
    )
    {
        var pigooraConfig = "_appConfig.PigooraConfig";
        var cekResiUrl = "pigooraConfig.CekResiUrl";
        var cekResiToken = "pigooraConfig.CekResiToken";

        var cekResi = await PigooraCekResiUrl
            .SetQueryParam("key", cekResiToken)
            .SetQueryParam("resi", awb)
            .SetQueryParam("kurir", courier)
            .SetQueryParams()
            .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:85.0) Gecko/20100101 Firefox/85.0")
            .WithHeader("Host", "api.cekresi.pigoora.com")
            .WithHeader("Origin", "https://cekresi.pigoora.com")
            .WithHeader("Referer", "https://cekresi.pigoora.com/")
            .GetJsonAsync<PigooraCekResi>();
    }
}