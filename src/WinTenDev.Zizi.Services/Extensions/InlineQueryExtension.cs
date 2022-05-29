using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
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

        var reducedResult = inlineQueryResults.Take(50);

        try
        {
            await telegramService.Client.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryId,
                results: reducedResult
            );
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error when answering inline query: {Id}",
                inlineQueryId
            );
        }
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
            "subscene" => await telegramService.OnInlineQuerySubsceneSearchAsync(),
            "subscene-dl" => await telegramService.OnInlineQuerySubsceneDownloadAsync(),
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

    private static async Task<InlineQueryExecutionResult> OnInlineQuerySubsceneSearchAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();
        var queryCmd = telegramService.GetInlineQueryAt<string>(0);
        var queryValue = telegramService.InlineQuery.Query.Replace(queryCmd, "").Trim();
        Log.Information("Starting find Subtitle with title: '{QueryValue}'", queryValue);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();
        var searchByTitle = await subsceneService.SearchByTitle(queryValue);
        if (searchByTitle.Count == 0)
        {
            await telegramService.AnswerInlineQueryAsync(
                new List<InlineQueryResult>()
                {
                    new InlineQueryResultArticle(
                        id: Guid.NewGuid().ToString(),
                        title: "Tidak di temukan hasil, silakan cari judul yang lain",
                        inputMessageContent: new InputTextMessageContent("Tekan tombol dibawah ini untuk memulai pencarian")
                    )
                    {
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene")
                            }
                        )
                    }
                }
            );
            executionResult.IsSuccess = false;

            return executionResult;
        }

        var result = searchByTitle.Select(
            element => {
                Log.Debug("Appending Movie: '{0}'", element.Text);
                var slug = element.PathName.Split("/").LastOrDefault();

                var article = new InlineQueryResultArticle(
                    id: Guid.NewGuid().ToString(),
                    title: element.Text,
                    inputMessageContent: new InputTextMessageContent(element.Text)
                )
                {
                    ReplyMarkup = new InlineKeyboardMarkup(
                        new[]
                        {
                            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai unduh", "subscene-dl " + slug),
                            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene")
                        }
                    )
                };

                return article;
            }
        );

        await telegramService.AnswerInlineQueryAsync(result);
        executionResult.IsSuccess = true;

        return executionResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQuerySubsceneDownloadAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();
        var queryCmd = telegramService.GetInlineQueryAt<string>(0);
        var query1 = telegramService.GetInlineQueryAt<string>(1);
        var query2 = telegramService.GetInlineQueryAt<string>(2);
        Log.Information("Starting find Subtitle file with title: '{QueryValue}'", query1);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();
        var searchBySlug = await subsceneService.SearchBySlug(query1);
        var filteredSearch = searchBySlug.Where(
            element => {
                Log.Debug("Subtitle list element: {@A}", element);
                var rowText = element.Text.ToLower().Replace("\t", " ");
                return rowText.Contains(query2);
            }
        ).ToList();

        Log.Information(
            "Found {FilteredCount} of {AllCount} subtitle with title: '{QueryValue}'",
            filteredSearch.Count,
            searchBySlug.Count,
            query2
        );

        if (filteredSearch.Count == 0)
        {
            await telegramService.AnswerInlineQueryAsync(
                new List<InlineQueryResult>()
                {
                    new InlineQueryResultArticle(
                        id: Guid.NewGuid().ToString(),
                        title: "Tidak di temukan hasil, silakan cari bahasa/judul yang lain",
                        inputMessageContent: new InputTextMessageContent("Tekan tombol dibawah ini untuk memulai pencarian")
                    )
                    {
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene")
                            }
                        )
                    }
                }
            );
            executionResult.IsSuccess = false;

            return executionResult;
        }

        var result = filteredSearch.Select(
            element => {
                Log.Debug("Appending Movie: '{0}'", element.Text);
                var title = element.Text.Replace("\t", " ").Replace("\n", "");
                var content = element.Text.Replace("\t", " ");
                var slug = element.PathName.Split("/").Skip(2).JoinStr("/");

                var article = new InlineQueryResultArticle(
                    id: Guid.NewGuid().ToString(),
                    title: title,
                    inputMessageContent: new InputTextMessageContent(content)
                )
                {
                    ReplyMarkup = new InlineKeyboardMarkup(
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Mulai unduh file", "subscene-dl"),
                            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene")
                        }
                    )
                };

                return article;
            }
        );

        await telegramService.AnswerInlineQueryAsync(result);

        executionResult.IsSuccess = true;

        return executionResult;
    }

}
