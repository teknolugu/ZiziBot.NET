using Microsoft.AspNetCore.Mvc;

namespace WinTenDev.ZiziApi.AppHost.Controllers;

[Route("")]
[ApiController]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(
            new
            {
                Message = "Welcome to Zizi API",
                Version = AssemblyUtil.GetVersionNumber()
            }
        );
    }
}