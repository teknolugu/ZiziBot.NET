using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Extensions;

public static class InlineQueryExtension
{
    private static async Task AnswerInlineQueryAsync(
        this TelegramService telegramService,
        IEnumerable<InlineQueryResult> inlineQueryResults
    )
    {
        var inlineQuery = telegramService.InlineQuery;
        var inlineQueryId = inlineQuery.Id;

        await telegramService.Client.AnswerInlineQueryAsync(
            inlineQueryId: inlineQueryId,
            results: inlineQueryResults
        );
    }

    public static async Task OnInlineQueryAsync(this TelegramService telegramService)
    {
        var inlineQuery = telegramService.InlineQuery;
        Log.Debug("InlineQuery: {@Obj}", inlineQuery);

        var inlineQueryCmd = telegramService.GetInlineQueryAt<string>(0).Trim();

        var inlineQueryExecutionResult = inlineQueryCmd switch
        {
            "ping" => await telegramService.OnInlineQueryPingAsync(),
            "message" => await telegramService.OnInlineQueryMessageAsync(),
            _ => await telegramService.OnInlineQueryGuideAsync()
        };

        inlineQueryExecutionResult.Stopwatch.Stop();

        Log.Debug("Inline Query execution result: {@Result}", inlineQueryExecutionResult);
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQueryGuideAsync(this TelegramService telegramService)
    {
        var learnMore = "https://docs.zizibot.winten.my.id/features/inline-query";
        var inlineResult = new InlineQueryExecutionResult();

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                new InlineQueryResultArticle(
                    id: "guide-1",
                    title: "Bagaimana cara menggunakannya?",
                    inputMessageContent: new InputTextMessageContent($"Silakan pelajari selengkapnya\n{learnMore}")
                ),
                new InlineQueryResultArticle(
                    id: "guide-2",
                    title: "Cobalah ketikkan 'ping'",
                    inputMessageContent: new InputTextMessageContent($"Silakan pelajari selengkapnya\n{learnMore}")
                )
            }
        );

        inlineResult.IsSuccess = true;

        return inlineResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQueryPingAsync(this TelegramService telegramService)
    {
        var inlineResult = new InlineQueryExecutionResult();

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                new InlineQueryResultArticle(
                    "ping-result",
                    "Pong!",
                    new InputTextMessageContent("Pong!")
                )
            }
        );

        inlineResult.IsSuccess = true;

        return inlineResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQueryMessageAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();

        var inlineQuery = telegramService.InlineQuery.Query;
        var parseMessage = inlineQuery
            .Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.Contains('='))
            .ToDictionary(
                s => s.Split('=')[0],
                s => s.Split('=')[1]
            );

        if (parseMessage.Count == 0)
        {
            var learnMore = "Pelajari cara membuat tombol dengan InlineQuery";
            var urlArticle = "https://docs.zizibot.winten.my.id/features/inline-query/pesan-dengan-tombol";

            await telegramService.AnswerInlineQueryAsync(
                new List<InlineQueryResult>()
                {
                    new InlineQueryResultArticle(
                        "iq-learn-mode",
                        learnMore,
                        new InputTextMessageContent(learnMore + $"\n{urlArticle}")
                    )
                }
            );

            return executionResult;
        }

        var caption = parseMessage.GetValueOrDefault("caption", string.Empty);
        var replyMarkup = parseMessage.GetValueOrDefault("button").ToInlineKeyboardButton().ToButtonMarkup();

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                new InlineQueryResultArticle(
                    "123",
                    caption,
                    new InputTextMessageContent(caption)
                )
                {
                    ReplyMarkup = replyMarkup
                }
            }
        );

        executionResult.IsSuccess = true;

        return executionResult;
    }
}
