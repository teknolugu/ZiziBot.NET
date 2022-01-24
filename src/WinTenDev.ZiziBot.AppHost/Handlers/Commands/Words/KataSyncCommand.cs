using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Words;

public class KataSyncCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly WordFilterService _wordFilterService;

    public KataSyncCommand(
        TelegramService telegramService,
        WordFilterService wordFilterService
    )
    {
        _telegramService = telegramService;
        _wordFilterService = wordFilterService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var isSudoer = _telegramService.IsFromSudo;

        await _telegramService.DeleteSenderMessageAsync();

        if (!isSudoer)
        {
            Log.Debug("Only sudo can do Sync Kata!");
            return;
        }

        await _telegramService.AppendTextAsync("Sedang mengsinkronkan Word Filter");
        await _wordFilterService.UpdateWordListsCache();

        await _telegramService.AppendTextAsync("Selesai mengsinkronkan.");

        await _telegramService.DeleteAsync(delay: 3000);
    }
}