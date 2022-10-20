using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Serilog;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceSpellExtension
{
    public static async Task AddSpellAsync(this TelegramService telegramService)
    {
        if (!telegramService.IsFromSudo)
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        var spellService = telegramService.GetRequiredService<SpellService>();
        var typo = telegramService.GetCommandParam(0);
        var fix = telegramService.GetCommandParam(1);
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;

        var spellDto = new SpellDto()
        {
            Typo = typo,
            Fix = fix,
            ChatId = chatId,
            FromId = fromId,
        };

        if (!(await spellDto.ValidateAsync<AddSpellDtoValidator, SpellDto>()).IsValid)
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
        var saveSpell = await spellService.SaveSpell(spellDto);

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

    public static async Task ImportSpellAsync(this TelegramService telegramService)
    {
        if (!telegramService.IsFromSudo)
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        if (telegramService.ReplyToMessage == null)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Balas pesan yang berisi file spell",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                includeSenderMessage: true
            );

            return;
        }

        var spellService = telegramService.GetRequiredService<SpellService>();

        var fileName = await telegramService.DownloadFileAsync("spell");

        var csvRows = fileName.ReadCsv<SpellDto>();

        var htmlMessage = HtmlMessage.Empty
            .BoldBr("⏬ Import Spell");

        try
        {
            var importSpell = await spellService.ImportSpell(csvRows);

            htmlMessage.TextBr("Spell berhasil diimport")
                .Bold("Total: ").CodeBr(importSpell.ToString());

            await telegramService.SendTextMessageAsync(
                htmlMessage.ToString(),
                includeSenderMessage: true,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5));
        }
        catch (MongoBulkWriteException<SpellEntity> bulkWriteException)
        {
            var writeResult = bulkWriteException.Result;

            if (writeResult.InsertedCount > 0)
            {
                htmlMessage.TextBr("Spell berhasil diimport")
                    .Bold("Ditambahkan: ").CodeBr(writeResult.InsertedCount.ToString())
                    .Bold("Dilewat: ").CodeBr(bulkWriteException.WriteErrors.Count.ToString());
            }
            else
            {
                htmlMessage.TextBr("Spell telah diimport")
                    .Bold("Total: ").CodeBr(bulkWriteException.WriteErrors.Count.ToString());
            }

            await telegramService.SendTextMessageAsync(
                htmlMessage.ToString(),
                includeSenderMessage: true,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5)
            );
        }
        catch (Exception ex)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Terjadi kesalahan pada saat import spell",
                includeSenderMessage: true,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5)
            );
        }
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