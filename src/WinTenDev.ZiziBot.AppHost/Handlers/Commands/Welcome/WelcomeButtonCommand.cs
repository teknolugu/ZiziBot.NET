using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;

public class WelcomeButtonCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly SettingsService _settingsService;

    public WelcomeButtonCommand(
        TelegramService telegramService,
        SettingsService settingsService)
    {
        _telegramService = telegramService;
        _settingsService = settingsService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;
        var chatId = _telegramService.ChatId;

        if (_telegramService.IsPrivateChat) return;

        if (!await _telegramService.CheckFromAdmin()) return;

        var columnTarget = $"welcome_button";
        var data = msg.Text.GetTextWithoutCmd();

        if (msg.ReplyToMessage != null)
        {
            data = msg.ReplyToMessage.Text;
        }

        if (data.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync($"Silakan masukan konfigurasi Tombol yang akan di terapkan");
            return;
        }

        await _telegramService.SendTextMessageAsync($"Sedang menyimpan Welcome Button..");

        await _settingsService.UpdateCell(chatId, columnTarget, data);

        await _telegramService.EditMessageTextAsync($"Welcome Button berhasil di simpan!" +
                                                    $"\nKetik /welcome untuk melihat perubahan");

        Log.Information("Success save welcome Button on {ChatId}.", chatId);
    }
}