using DalSoft.Hosting.BackgroundQueue;
using Microsoft.AspNetCore.Mvc;

namespace WinTenDev.ZiziApi.AppHost.Controllers;

[Route("[controller]")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly BackgroundQueue _backgroundQueue;
    private readonly WebHookService _webHookService;

    public WebhookController(
        BackgroundQueue backgroundQueue,
        WebHookService webHookService
    )
    {
        _backgroundQueue = backgroundQueue;
        _webHookService = webHookService;
    }

    [HttpPost]
    [Route("{targetId}")]
    public async Task<IActionResult> Run(
        [FromBody] object content,
        string targetId
    )
    {
        var webhookDto = new WebhookDto
        {
            HookId = Request.RouteValues.ElementAtOrDefault(2).Value?.ToString(),
            BodyString = await Request.GetRawBodyAsync(),
            WebhookSource = Request.GetWebHookSource(),
            Headers = Request.Headers,
            Query = Request.Query,
            RequestOn = (DateTime)Request.HttpContext.Items["RequestStartedOn"]!,
            HttpRequest = Request
        };

        _backgroundQueue.Enqueue(token => {

            var result = _webHookService.ProcessingRequest(webhookDto);
            return result;
        });

        return Ok(true);
    }
}