using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Telegram;

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

        var inlineQueryCmd = telegramService.GetInlineQueryAt<string>(0);

        var inlineQueryExecutionResult = inlineQueryCmd switch
        {
            "ping" => await telegramService.OnInlineQueryPingAsync(),
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
}
