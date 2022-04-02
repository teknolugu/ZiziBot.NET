using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TgBotFramework;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.Alpha2.Handlers;

public class NewUpdateHandler : IUpdateHandler<UpdateContext>
{
    private readonly ILogger<NewUpdateHandler> _logger;
    private readonly TelegramService _telegramService;

    public NewUpdateHandler(
        ILogger<NewUpdateHandler> logger,
        TelegramService telegramService
    )
    {
        _logger = logger;
        _telegramService = telegramService;

    }

    public async Task HandleAsync(
        UpdateContext context,
        UpdateDelegate<UpdateContext> next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        var preTask = await _telegramService.OnUpdatePreTaskAsync();

        // Last, do additional task which bot may do
        _telegramService.OnUpdatePostTaskAsync().InBackground();

        if (!preTask)
        {
            _logger.LogDebug("Next handler is ignored because pre-task is not success");
            return;
        }

        _logger.LogDebug("Continue to next Handler");

        await next(context, cancellationToken);
    }
}