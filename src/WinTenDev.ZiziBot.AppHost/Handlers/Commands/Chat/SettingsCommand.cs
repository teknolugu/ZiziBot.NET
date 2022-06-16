using System;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Chat;

public class SettingsCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly SettingsService _settingsService;

    public SettingsCommand(
        SettingsService settingsService,
        TelegramService telegramService
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

        var chatId = _telegramService.ChatId;
        var fromId = _telegramService.FromId;

        await _telegramService.DeleteSenderMessageAsync();

        if (!await _telegramService.CheckUserPermission())
        {
            Log.Warning(
                "UserId: {UserId} on ChatId: {ChatId} not have permission to modify Settings",
                fromId,
                chatId
            );
            return;
        }

        await _telegramService.SendTextMessageAsync("Sedang mengambil pengaturan..", replyToMsgId: 0);
        var settings = await _settingsService.GetSettingButtonByGroup(chatId);

        var btnMarkup = await settings.ToJson().JsonToButton(chunk: 2);
        Log.Debug("Settings: {Count}", settings.Count);

        await _telegramService.EditMessageTextAsync(
            sendText: "Pengaturan Obrolan",
            btnMarkup,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10)
        );
    }
}
