using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WinTenDev.Zizi.Services.Internals;

namespace WinTenDev.WebApi.AppHost.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WordFilterController : Controller
    {
        private readonly WordFilterService _wordFilterService;

        public WordFilterController(
            WordFilterService wordFilterService,
            QueryService queryService
        )
        {
            _wordFilterService = wordFilterService;
        }

        // GET
        public async Task<IActionResult> Index()
        {
            var data = await _wordFilterService.GetWordsList();

            return Json(data);
        }
    }
}