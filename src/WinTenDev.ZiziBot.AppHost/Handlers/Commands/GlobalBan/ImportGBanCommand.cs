using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.GlobalBan;

public class ImportGBanCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly GlobalBanService _globalBanService;

    public ImportGBanCommand(
        TelegramService telegramService,
        GlobalBanService globalBanService
    )
    {
        _telegramService = telegramService;
        _globalBanService = globalBanService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);
        var fromId = _telegramService.FromId;
        var chatId = _telegramService.ChatId;
        var messageTexts = _telegramService.MessageTextParts;
        var reasonBan = messageTexts.ElementAtOrDefault(1) ?? "Import from Bot";

        if (!_telegramService.IsFromSudo)
        {
            await _telegramService.DeleteSenderMessageAsync();
        }

        var repMessage = _telegramService.ReplyToMessage;

        if (repMessage.Type != MessageType.Document)
        {
            await _telegramService.SendTextMessageAsync("Reply persan dokument untuk Import Global Ban. Biasanya berkas .xsv atau .json");
        }

        var document = repMessage.Document;
        var documentFileName = document.FileName;

        await _telegramService.AppendTextAsync("Mengambil berkas..");
        var fileName = await _telegramService.DownloadFileAsync("import_gban");

        await _telegramService.AppendTextAsync($"Menguraikan berkas {documentFileName}");

        var import = await _globalBanService.ImportFile
        (
            fileName,
            new GlobalBanItem()
            {
                ReasonBan = reasonBan,
                BannedBy = fromId,
                BannedFrom = chatId
            }
        );

        if (import > 0)
        {
            await _telegramService.AppendTextAsync($"Sebanyak {import} item berhasil ditambahkan");
        }
        else
        {
            await _telegramService.AppendTextAsync($"Semua data sudah ditambahkan");
        }
    }
}