using System.Threading.Tasks;
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

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var isSudoer = _telegramService.IsFromSudo;
        var isAdmin = await _telegramService.CheckFromAdmin();

        if (!isSudoer)
        {
            return;
        }

        await _telegramService.DeleteAsync(_telegramService.Message.MessageId);

        await _telegramService.AppendTextAsync("Sedang mengsinkronkan Word Filter");
        await _wordFilterService.UpdateWordsCache();
// /            await _queryFactory.SyncWordToLocalAsync();


        await _telegramService.AppendTextAsync("Selesai mengsinkronkan.");

        await _telegramService.DeleteAsync(delay: 3000);
    }
}