using System.Threading;
using System.Threading.Tasks;
using TgBotFramework;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;

namespace WinTenDev.ZiziBot.Alpha2.Handlers.Chat;

public class AddNoteCommand : CommandBase<UpdateContext>
{
    private readonly TelegramService _telegramService;

    public AddNoteCommand(TelegramService telegramService)
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
        await _telegramService.PrepareSaveNotesAsync();
    }
}