using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SqlKata.Execution;
using WinTenDev.WebApi.AppHost.Models;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.WebApi.AppHost.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopTrendsController : Controller
    {
        private readonly QueryService _queryService;

        public TopTrendsController(
            QueryService queryService
        )
        {
            _queryService = queryService;
        }

        [HttpGet("top-30d")]
        public async Task<JsonResult> TopActivityLast30Days()
        {
            var stopwatch = Stopwatch.StartNew();

            var model = await _queryService
                .CreateMySqlFactory()
                .FromTable("zizibot_data.view_top_hit_activity_last_30d")
                .GetAsync<TopTrendActivity>();

            var topTrendActivities = model.ToList();

            stopwatch.Stop();
            Log.Information("Elapsed. {Elapsed}", stopwatch.Elapsed);

            return Json(new
            {
                Data = topTrendActivities,
                Count = topTrendActivities.Count
            });
        }
    }
}