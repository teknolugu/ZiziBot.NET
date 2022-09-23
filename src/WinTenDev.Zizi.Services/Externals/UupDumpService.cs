using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace WinTenDev.Zizi.Services.Externals;

public class UupDumpService
{
    private readonly ILogger<UupDumpService> _logger;
    private readonly CacheService _cacheService;
    private const string ListUpdatesApi = "https://api.uupdump.net/listid.php";

    public UupDumpService(
        ILogger<UupDumpService> logger,
        CacheService cacheService
    )
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<BuildUpdate> GetUpdatesAsync(string search = "")
    {
        var buildUpdate = await _cacheService.GetOrSetAsync(
            cacheKey: "uup_" + ListUpdatesApi.ToCacheKey(),
            action: async () => {
                var obj = await ListUpdatesApi.OpenFlurlSession()
                    .GetJsonAsync<BuildUpdate>();

                return obj;
            }
        );

        var filteredBuilds = buildUpdate.Response.Builds
            .Where(build => build.BuildNumber.Contains(search))
            .ToList();

        _logger.LogDebug("Retrieve about {Count} build(s)", buildUpdate.Response.Builds.Count);

        var filteredUpdate = new BuildUpdate
        {
            JsonApiVersion = buildUpdate.JsonApiVersion,
            Response = new Response
            {
                ApiVersion = buildUpdate.Response.ApiVersion,
                Builds = filteredBuilds
            }
        };

        return filteredUpdate;
    }
}