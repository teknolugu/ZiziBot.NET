using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class BackupDbCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly DatabaseService _databaseService;

    public BackupDbCommand(
        TelegramService telegramService,
        DatabaseService databaseService
    )
    {
        _telegramService = telegramService;
        _databaseService = databaseService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var isSudoer = _telegramService.IsFromSudo;

        if (!isSudoer)
        {
            await _telegramService.DeleteSenderMessageAsync();
            return;
        }

        ExecuteBackupDb().InBackground();
    }

    private async Task ExecuteBackupDb()
    {
        await _telegramService.SendTextMessageAsync("⬇ Sedang mencadangkan Database..", replyToMsgId: 0);

        var dataBackupInfo = await _databaseService.BackupMySqlDatabase();
        var fullNameZip = dataBackupInfo.FullNameZip;

        var sentMessage = await _telegramService.EditMessageTextAsync("⬆ Sedang mengunggah berkas..");

        var caption = $"File Size: {dataBackupInfo.FileSizeSql}";

        await _telegramService.SendMediaAsync(fullNameZip, MediaType.LocalDocument, caption);
        await _telegramService.DeleteAsync(sentMessage.MessageId);
    }
}