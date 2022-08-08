using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceSubtitleExtension
{
    public static async Task AddSubtitleSource(this TelegramService telegramService)
    {
        if (!telegramService.IsFromSudo)
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;

        var subsceneTitleUrl = telegramService.GetCommandParamAt<string>(0);
        var searchSubtitleUrl = telegramService.GetCommandParamAt<string>(1);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();

        var subsceneSource = new SubsceneSource()
        {
            ChatId = chatId,
            UserId = fromId,
            SearchTitleUrl = subsceneTitleUrl,
            SearchSubtitleUrl = searchSubtitleUrl,
            IsActive = false
        };

        var validationResult = await subsceneSource.ValidateAsync<AddSubsceneSourceValidator, SubsceneSource>();

        var htmlMessage = HtmlMessage.Empty
            .Bold("Add Subscene Source").Br();

        if (!validationResult.IsValid)
        {
            validationResult.Errors.ForEach(failure => {
                htmlMessage.TextBr(failure.ErrorMessage);
            });

            await telegramService.SendTextMessageAsync(htmlMessage.ToString());

            return;
        }

        await subsceneService.SaveSourceUrl(subsceneSource);

        htmlMessage
            .Bold("Title: ").TextBr(subsceneSource.SearchTitleUrl)
            .Bold("Subtitle: ").TextBr(subsceneSource.SearchSubtitleUrl);

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(3),
            includeSenderMessage: true
        );
    }

    public static async Task GetSubtitleSources(this TelegramService telegramService)
    {
        if (!telegramService.IsFromSudo)
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();

        var sources = await subsceneService.GetSourcesAsync();

        var replyMarkup = InlineKeyboardMarkup.Empty();

        var htmlMessage = HtmlMessage.Empty
            .Bold("Subscene Sources").Br();

        if (sources.Count == 0)
        {
            htmlMessage.TextBr("No sources found.");
        }
        else
        {
            htmlMessage.Bold("Total: ").TextBr(sources.Count.ToString());

            replyMarkup = await subsceneService.GetSourcesAsButtonAsync();
        }

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            replyMarkup: replyMarkup,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(3),
            includeSenderMessage: true
        );
    }

    public static async Task<bool> OnCallbackSelectSubsceneSourceAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var sourceId = telegramService.GetCallbackDataAt<string>(1);

        if (!telegramService.IsFromSudo)
        {
            Log.Information(
                "UserId: '{UserId}' at ChatId: '{ChatId}' has no permission to delete message",
                fromId,
                chatId
            );

            await telegramService.AnswerCallbackQueryAsync("Kamu tidak mempunyai akses melakukan tindakan ini!", true);
            return true;
        }

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();

        await subsceneService.SetSourceUrlAsync(sourceId);

        var replyMarkup = InlineKeyboardMarkup.Empty();

        var htmlMessage = HtmlMessage.Empty
            .Bold("Subscene Sources").Br();

        replyMarkup = await subsceneService.GetSourcesAsButtonAsync();
        htmlMessage.Bold("Total: ").TextBr(replyMarkup.InlineKeyboard.Count().ToString());

        await telegramService.EditMessageCallback(htmlMessage.ToString(), replyMarkup);

        var activeSource = await subsceneService.GetActiveSourceAsync();

        await telegramService.AnswerCallbackQueryAsync("Sumber berhasil diperbarui!", true);

        return true;
    }
}