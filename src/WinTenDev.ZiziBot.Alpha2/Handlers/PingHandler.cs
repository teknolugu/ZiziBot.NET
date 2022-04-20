using System.Threading;
using System.Threading.Tasks;
using TgBotFramework;

namespace WinTenDev.ZiziBot.Alpha2.Handlers;

public class PingHandler : IUpdateHandler<UpdateContext>
{
    private readonly TelegramService _telegramService;

    public PingHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        UpdateContext context,
        UpdateDelegate<UpdateContext> next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.SendPingAsync();
    }
}
