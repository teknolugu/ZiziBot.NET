using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class NewUpdateHandler : IUpdateHandler
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
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        _logger.LogTrace("NewUpdate: {@V}", _telegramService.Update);

        // Pre-Task is should be awaited.
        var preTaskResult = await _telegramService.OnUpdatePreTaskAsync();

        // Last, do additional task which bot may do
        _telegramService.OnUpdatePostTaskAsync().InBackground();

        if (!preTaskResult)
        {
            _logger.LogInformation(
                "Next handler at ChatId: {ChatId} is ignored because pre-task is not success. UpdateType: {UpdateType}",
                _telegramService.ChatId,
                _telegramService.Update.Type
            );
            return;
        }

        _logger.LogDebug("Continue to next Handler");

        await next(context, cancellationToken);
    }
}
