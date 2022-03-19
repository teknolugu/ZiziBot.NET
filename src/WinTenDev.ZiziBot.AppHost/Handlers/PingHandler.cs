using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

internal class PingHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public PingHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.SendPingAsync();
    }
}