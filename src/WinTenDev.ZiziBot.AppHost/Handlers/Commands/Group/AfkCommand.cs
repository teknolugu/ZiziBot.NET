using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class AfkCommand : CommandBase
{
    private readonly SettingsService _settingsService;
    private readonly AfkService _afkService;
    private readonly TelegramService _telegramService;

    public AfkCommand(
        SettingsService settingsService,
        TelegramService telegramService,
        AfkService afkService
    )
    {
        _settingsService = settingsService;
        _afkService = afkService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var fromNameLink = _telegramService.FromNameLink;
        var fromId = _telegramService.FromId;
        var chatId = _telegramService.ChatId;
        var afkReason = _telegramService.MessageOrEditedText.GetTextWithoutCmd();

        if (_telegramService.CheckFromAnonymous() ||
            _telegramService.CheckSenderChannel())
        {
            await _telegramService.SendTextMessageAsync("Mode AFK dimatikan untuk Pengguna Anonymous");
            return;
        }

        var settings = await _settingsService.GetSettingsByGroup(chatId);

        if (!settings.EnableAfkStatus)
        {
            await _telegramService.DeleteSenderMessageAsync();
            return;
        }

        var data = new Dictionary<string, object>()
        {
            { "user_id", fromId },
            { "chat_id", chatId },
            { "is_afk", 1 },
            { "afk_start", DateTime.Now },
            { "afk_end", DateTime.Now }
        };

        var sendText = $"{fromNameLink} Sedang afk.";

        if (afkReason.IsNotNullOrEmpty())
        {
            data.Add("afk_reason", afkReason);

            sendText += $"\n<i>{afkReason}</i>";
        }

        await _telegramService.SendTextMessageAsync(sendText);
        await _afkService.SaveAsync(data);
        await _afkService.UpdateAfkByIdCacheAsync(fromId);
    }
}