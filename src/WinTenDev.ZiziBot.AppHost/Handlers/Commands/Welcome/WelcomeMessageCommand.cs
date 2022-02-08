using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;

public class WelcomeMessageCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly SettingsService _settingsService;

    public WelcomeMessageCommand(
        TelegramService telegramService,
        SettingsService settingsService
    )
    {
        _telegramService = telegramService;
        _settingsService = settingsService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;
        var chatId = _telegramService.ChatId;

        if (!await _telegramService.CheckFromAdminOrAnonymous()) return;

        var columnTarget = $"welcome_message";
        var data = msg.Text.GetTextWithoutCmd();

        if (msg.ReplyToMessage != null)
        {
            data = msg.ReplyToMessage.Text;
        }

        if (data.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync($"Silakan masukan konfigurasi Pesan yang akan di terapkan");
            return;
        }

        await _telegramService.SendTextMessageAsync($"Sedang menyimpan Welcome Message..");

        await _settingsService.UpdateCell(chatId, columnTarget, data);

        await _telegramService.EditMessageTextAsync
        (
            $"Welcome Button berhasil di simpan!" +
            $"\nKetik /welcome untuk melihat perubahan"
        );

        Log.Information("Success save welcome Message on {ChatId}.", chatId);
    }
}