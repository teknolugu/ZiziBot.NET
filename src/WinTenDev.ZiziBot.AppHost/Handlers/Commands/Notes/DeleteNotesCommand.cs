using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Notes;

public class DeleteNotesCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public DeleteNotesCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.DeleteNoteAsync();
    }
}
