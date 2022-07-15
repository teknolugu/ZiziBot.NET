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
                Success = true,
                Message = "Welcome to Zizi API",
                Version = "1.0.0.0"
            }
        );
    }
}