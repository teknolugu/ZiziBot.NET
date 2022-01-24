using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Words;

public class DeleteKataCommand : CommandBase
{
    private readonly ILogger _logger;
    private readonly TelegramService _telegramService;
    private readonly WordFilterService _wordFilterService;

    public DeleteKataCommand(
        ILogger logger,
        TelegramService telegramService,
        WordFilterService wordFilterService
    )
    {
        _logger = logger;
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

        var chatId = _telegramService.ChatId;
        var messageTexts = _telegramService.MessageTextParts;
        var word = messageTexts.ElementAtOrDefault(1);
        var isSudo = _telegramService.IsFromSudo;

        if (!isSudo)
        {
            _logger.Warning("Delete Kata currently only for Sudo!");
            await _telegramService.DeleteAsync();
            return;
        }

        if (word.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync("Kata apa yang mau di hapus?");
        }

        await _telegramService.SendTextMessageAsync("Sedang menghapus Kata..");

        var wordFilter = new WordFilter()
        {
            Word = word
        };

        var delete = await _wordFilterService.DeleteKata(wordFilter);

        var deleteResult = delete > 0 ? "Kata berhasil di hapus" : "Kata sudah dihapus";
        await _telegramService.EditMessageTextAsync(deleteResult);

        await _wordFilterService.UpdateWordListsCache();
    }
}