using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Notes;

public class AddNoteCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public AddNoteCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        await _telegramService.PrepareSaveNotesAsync();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        await next(context, cts.Token);
    }
}