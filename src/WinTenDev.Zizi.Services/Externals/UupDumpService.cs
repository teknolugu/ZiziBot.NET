using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Serilog;
using WinTenDev.Zizi.Models.Types.UupDump;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Externals;

public class UupDumpService
{
    private readonly ILogger _logger;
    private readonly CacheService _cacheService;
    private const string ListUpdatesApi = "https://api.uupdump.net/listid.php";

    public UupDumpService(
        ILogger logger,
        CacheService cacheService
    )
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<BuildUpdate> GetUpdatesAsync(string search = "")
    {
        var buildUpdate = await _cacheService.GetOrSetAsync(
            cacheKey: ListUpdatesApi,
            action: async () => {
                var obj = await ListUpdatesApi.OpenFlurlSession()
                    .GetJsonAsync<BuildUpdate>();

                return obj;
            }
        );

        var filteredBuilds = buildUpdate.Response.Builds
            .Where(build => build.BuildNumber.Contains(search))
            .ToList();

        _logger.Debug("Retrieve about {0} build(s)", buildUpdate.Response.Builds.Count);

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
