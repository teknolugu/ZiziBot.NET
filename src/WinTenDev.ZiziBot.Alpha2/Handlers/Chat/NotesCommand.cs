using System.Threading;
using System.Threading.Tasks;
using TgBotFramework;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;

namespace WinTenDev.ZiziBot.Alpha2.Handlers.Chat;

public class NotesCommand : CommandBase<UpdateContext>
{
    private readonly TelegramService _telegramService;

    public NotesCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;

    }

    public override async Task HandleAsync(
        UpdateContext context,
        UpdateDelegate<UpdateContext> next,
        string[] args,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.GetNotesAsync();
    }
}