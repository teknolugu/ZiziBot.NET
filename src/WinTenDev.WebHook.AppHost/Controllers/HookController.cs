using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WinTenDev.WebHook.AppHost.Services;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.WebHook.AppHost.Controllers
{
    [ApiController]
    [Route("/")]
    public class HookController : ControllerBase
    {
        private readonly ITelegramService _telegramService;
        private readonly GitHubService _gitHubService;

        public HookController(
            ITelegramService telegramServiceService,
            GitHubService gitHubService
        )
        {
            _telegramService = telegramServiceService;
            _gitHubService = gitHubService;
        }

        // GET
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Jangkrik");
        }

        [HttpPost("/debug")]
        public IActionResult Debug([FromBody] object content, [FromQuery] object query)
        {
            var stopwatch = Stopwatch.StartNew();

            var jsonName = HttpContext.TraceIdentifier + ".json";
            Log.Information("Receiving hook");

            // Log.Debug("Content: {Content}", content);
            Log.Debug("Query: {@Query}", query);

            var headers = HttpContext.Request.Headers;
            var userAgent = headers["User-Agent"];
            var json = content.ToString() ?? string.Empty;
            var msgText = string.Empty;

            if (userAgent.Contains("GitHub"))
            {
                msgText = _gitHubService.ExecuteAsync(json);
            }
            else
            {
                Log.Information("Unknown Hook");
            }

            content.WriteToFileAsync(jsonName);

            // if (content == null)
            // {
            //     Log.Information("Content is null!");
            //     return Ok();
            // }

            _telegramService.SendMessage(-1001450455483, msgText);

            stopwatch.Stop();

            return new JsonResult(new
            {
                StatusCode = 200,
                Elapsed = stopwatch.Elapsed
            });
        }

        [HttpGet("/about")]
        public OkObjectResult About()
        {
            return Ok("About");
        }
    }
}