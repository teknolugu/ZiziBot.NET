using System.Threading;
using System.Threading.Tasks;
using TgBotFramework;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.Alpha2.Handlers;

public class GenericMessageHandler : IUpdateHandler<UpdateContext>
{
    private readonly TelegramService _telegramService;

    public GenericMessageHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;

    }

    public async Task HandleAsync(
        UpdateContext context,
        UpdateDelegate<UpdateContext> next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        await _telegramService.FindNoteAsync();
    }
}