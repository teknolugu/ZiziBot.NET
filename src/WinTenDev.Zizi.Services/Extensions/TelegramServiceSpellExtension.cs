using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceSpellExtension
{
    public static async Task AddSpellAsync(this TelegramService telegramService)
    {
        var spellService = telegramService.GetRequiredService<SpellService>();
        var typo = telegramService.GetCommandParam(0);
        var fix = telegramService.GetCommandParam(1);
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;

        var spellData = new Spell()
        {
            Typo = typo,
            Fix = fix,
            ChatId = chatId,
            FromId = fromId,
            CreatedAt = DateTime.UtcNow
        };

        if (!spellData.Validate<AddSpellValidator, Spell>().IsValid)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Sepertinya input Spell tidak valid" +
                          "\n<code>/add_spell {typo} {fix}</code>",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                includeSenderMessage: true
            );

            return;
        }

        var htmlMessage = HtmlMessage.Empty;
        var saveSpell = await spellService.SaveSpell(
            new Spell()
            {
                Typo = typo,
                Fix = fix,
                ChatId = chatId,
                FromId = fromId,
                CreatedAt = DateTime.UtcNow
            }
        );

        if (saveSpell)
        {
            htmlMessage.TextBr("Spell berhasil disimpan")
                .Bold("Typo: ").CodeBr(typo)
                .Bold("Fix: ").CodeBr(fix);
        }
        else
        {
            htmlMessage.TextBr("Spell sudah disimpan");
        }

        await spellService.GetSpellAll(evictBefore: true);

        await telegramService.AppendTextAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
            includeSenderMessage: true
        );
    }

    public static async Task RunSpellingAsync(this TelegramService telegramService)
    {
        var spellService = telegramService.GetRequiredService<SpellService>();
        var chatId = telegramService.ChatId;
        var messageText = telegramService.MessageOrEditedText ?? telegramService.MessageOrEditedCaption;

        if (messageText.IsNullOrEmpty())
        {
            Log.Debug("No message text for Spell check. ChatId: {ChatId}", chatId);
            return;
        }

        if (telegramService.GetCommand().IsNotNullOrEmpty())
        {
            Log.Debug("Spell check is disabled for command. ChatId: {ChatId}", chatId);
            return;
        }

        var chatSettings = await telegramService.GetChatSetting();

        if (!chatSettings.EnableSpellCheck)
        {
            Log.Debug("Spell check is disabled. ChatId: {ChatId}", chatId);
            return;
        }

        var spellsAll = await spellService.GetSpellAll();

        var fixedMessage = messageText.Split(" ")
            .Select(
                word =>
                    spellsAll.Any(spell => spell.Typo == word) ? spellsAll.First(spell => spell.Typo == word).Fix : word
            ).JoinStr(" ");

        var isTypoDetected = fixedMessage == messageText;

        if (isTypoDetected)
        {
            Log.Debug("No message typo detected at ChatId: {ChatId}", chatId);
            return;
        }

        var htmlMessage = HtmlMessage.Empty
            .BoldBr("Mungkin yang dimaksud adalah:")
            .Italic(fixedMessage);

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddDays(3),
            preventDuplicateSend: telegramService.EditedMessage != null,
            disableWebPreview: true
        );
    }
}