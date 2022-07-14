using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class GenericMessageHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public GenericMessageHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        _telegramService.FindNoteAsync().InBackground();
    }
}
