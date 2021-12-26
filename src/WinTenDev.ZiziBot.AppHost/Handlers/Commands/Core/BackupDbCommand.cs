using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class BackupDbCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly DataBackupService _dataBackupService;

    public BackupDbCommand(
        TelegramService telegramService,
        DataBackupService dataBackupService
    )
    {
        _telegramService = telegramService;
        _dataBackupService = dataBackupService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var isSudoer = _telegramService.IsFromSudo;
        if (!isSudoer) return;

        var message = _telegramService.MessageOrEdited;
        var chatId = _telegramService.ChatId;

        await _telegramService.SendTextMessageAsync("🔄 Sedang mencadangkan..");

        var dataBackupInfo = await _dataBackupService.BackupMySqlDatabase();
        var fileName = dataBackupInfo.FileName;
        var fullName = dataBackupInfo.FullName.GetDirectory();
        var fullNameZip = dataBackupInfo.FullNameZip;
        var fileSize = dataBackupInfo.FileSizeSqlZipRaw.SizeFormat();
        var fileSizeRaw = dataBackupInfo.FileSizeSqlRaw.SizeFormat();

        await _telegramService.EditMessageTextAsync("⬆ Sedang mengunggah..");
        await _telegramService.DeleteAsync();

        var caption = $"File Size: {dataBackupInfo.FileSizeSql}";

        await _telegramService.DeleteAsync(_telegramService.SentMessage.MessageId);
        await _telegramService.SendMediaAsync(fullNameZip, MediaType.LocalDocument, caption);
    }
}