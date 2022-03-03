using System;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class SetRulesCommand : CommandBase
{
    private readonly RulesService _rulesService;
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;
    private readonly PrivilegeService _privilegeService;

    public SetRulesCommand(
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
        var fromId = _telegramService.FromId;

        var message = _telegramService.MessageOrEdited;

        if (_telegramService.IsPrivateChat)
        {
            await _telegramService.DeleteSenderMessageAsync();

            return;
        }

        if (!await _telegramService.CheckFromAdminOrAnonymous())
        {
            await _telegramService.DeleteSenderMessageAsync();
            Log.Debug("Only admin can set rules at ChatId: {ChatId}", chatId);

            return;
        }

        _telegramService.SaveSenderMessageToHistory(MessageFlag.Rules, deleteAt);

        if (_telegramService.ReplyToMessage == null)
        {
            await _telegramService.SendTextMessageAsync("Balas sebuah pesan untuk disimpan sebagai Rules");
            _telegramService.SaveSentMessageToHistory(MessageFlag.Rules, deleteAt);

            return;
        }

        var text = message.CloneText();

        var rules = await _rulesService.SaveRuleAsync(
            new Rule
            {
                ChatId = chatId,
                FromId = fromId,
                RuleText = text,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        Log.Debug(
            "Save rules for ChatId {ChatId} result: {Rules}",
            chatId,
            rules
        );

        await _telegramService.SendTextMessageAsync("Rules berhasil disimpan");
        _telegramService.SaveSentMessageToHistory(MessageFlag.Rules, deleteAt);
    }
}