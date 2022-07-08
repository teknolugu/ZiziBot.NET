using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class CallbackQueryHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public CallbackQueryHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        _telegramService.OnCallbackReceiveAsync().InBackground();

        await next(context, cancellationToken);
    }
}
