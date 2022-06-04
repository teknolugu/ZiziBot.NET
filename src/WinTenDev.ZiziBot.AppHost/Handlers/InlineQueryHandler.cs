using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class InlineQueryHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public InlineQueryHandler(TelegramService telegramService)
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

        await _telegramService.OnInlineQueryAsync();
    }
}
