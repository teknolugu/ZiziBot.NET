using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class RulesCommand : CommandBase
{
    private readonly RulesService _rulesService;
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;
    private readonly PrivilegeService _privilegeService;

    public RulesCommand(
        RulesService rulesService,
        SettingsService settingsService,
        TelegramService telegramService,
        PrivilegeService privilegeService
    )
    {
        _rulesService = rulesService;
        _settingsService = settingsService;
        _telegramService = telegramService;
        _privilegeService = privilegeService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var deleteAt = DateTime.UtcNow.AddMinutes(5);
        var chatId = _telegramService.ChatId;
        var reducedChatId = _telegramService.ReducedChatId;

        if (_telegramService.IsPrivateChat)
        {
            Log.Debug("Rules only for Group");
            return;
        }
        var rules = await _rulesService.GetRulesAsync(chatId);

        var rulesText = rules.LastOrDefault()?.RuleText;

        var startUrl = await _telegramService.GetUrlStart($"start=rules_{reducedChatId}");

        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Klik disini", startUrl));

        var sendText = rulesText ?? "Sepertinya rules belum di tentukan untuk grup ini, namun bukan berarti Grup tanpa aturan!";

        _telegramService.SaveSenderMessageToHistory(MessageFlag.Rules, deleteAt);

        await _telegramService.SendTextMessageAsync(
            "Klik tombol dibawah ini untuk menadapatkan Rules",
            replyMarkup: keyboard,
            replyToMsgId: 0,
            disableWebPreview: true
        );

        _telegramService.SaveSentMessageToHistory(MessageFlag.Rules, deleteAt);
    }
}