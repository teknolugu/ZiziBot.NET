using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

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
        IEnumerable<InlineQueryResultArticle> result = null;
        var queryCmd = telegramService.GetInlineQueryAt<string>(0);
        var queryValue = telegramService.InlineQuery.Query.Replace(queryCmd, "").Trim();
        Log.Information("Starting find Subtitle with title: '{QueryValue}'", queryValue);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();

        if (queryValue.IsNotNullOrEmpty())
        {
            var searchByTitle = await subsceneService.GetOrFeedMovieByTitle(queryValue);

            if (searchByTitle == null)
            {
                var title = "Tidak di temukan hasil, silakan cari judul yang lain";
                if (queryValue.IsNullOrEmpty())
                {
                    title = "Silakan masukkan judul yang ingin dicari";
                }

                await subsceneService.FeedPopularTitles();
                await telegramService.AnswerInlineQueryAsync(
                    new List<InlineQueryResult>()
                    {
                        new InlineQueryResultArticle(
                            id: StringUtil.NewGuid(),
                            title: title,
                            inputMessageContent: new InputTextMessageContent("Tekan tombol dibawah ini untuk memulai pencarian")
                        )
                        {
                            ReplyMarkup = new InlineKeyboardMarkup(
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                                }
                            )
                        }
                    }
                );
                executionResult.IsSuccess = false;

                return executionResult;
            }

            Log.Information(
                "Found about {AllCount} title with query: '{QueryValue}'",
                searchByTitle.Count,
                queryValue
            );

            result = searchByTitle.Select(
                element => {
                    var movieTitle = element.MovieName;
                    var movieUrl = element.MovieUrl;
                    var subtitleCount = element.SubtitleCount;

                    Log.Debug(
                        "Appending MovieId: '{0}' => {1}",
                        movieUrl,
                        movieTitle
                    );

                    var slug = movieUrl.Split("/").LastOrDefault("subscene-slug" + StringUtil.GenerateUniqueId());
                    var titleHtml = HtmlMessage.Empty
                        .Bold("Title: ").CodeBr(movieTitle)
                        .Bold("Availability: ").CodeBr(subtitleCount)
                        .Bold("Url: ").Url($"https://subscene.com{movieUrl}", "Subscene Link");

                    var article = new InlineQueryResultArticle(
                        id: StringUtil.NewGuid(),
                        title: movieTitle,
                        inputMessageContent: new InputTextMessageContent(titleHtml.ToString())
                        {
                            ParseMode = ParseMode.Html,
                            DisableWebPagePreview = true
                        }
                    )
                    {
                        Description = $"Available subtitle: {subtitleCount}",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai unduh", $"subscene-dl {slug} "),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                            }
                        )
                    };

                    return article;
                }
            );
        }
        else
        {
            var popularTitles = await subsceneService.GetPopularMovieByTitle();
            result = popularTitles.Select(
                item => {
                    var movieTitle = item.MovieName;
                    var pathName = item.MovieUrl;
                    var moviePath = pathName.Split("/").Take(3).JoinStr("/");
                    var slug = pathName.Split("/").ElementAtOrDefault(2);

                    var titleHtml = HtmlMessage.Empty
                        .Bold("Title: ").CodeBr(movieTitle)
                        .Bold("Url: ").Url($"https://subscene.com{moviePath}", "Subscene Link");

                    var article = new InlineQueryResultArticle(
                        id: StringUtil.NewGuid(),
                        title: movieTitle,
                        inputMessageContent: new InputTextMessageContent(titleHtml.ToString())
                        {
                            ParseMode = ParseMode.Html,
                            DisableWebPagePreview = true
                        }
                    )
                    {
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai unduh", $"subscene-dl {slug} "),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                            }
                        )
                    };

                    return article;
                }
            );
        }

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
        var searchBySlug = await subsceneService.FeedSubtitleBySlug(query1);
        Log.Information(
            "Found about {AllCount} subtitle by slug: '{QueryValue}'",
            searchBySlug.Count,
            query1
        );

        var filteredSearch = searchBySlug.Where(
            element => {
                if (query2.IsNullOrEmpty()) return true;

                var rowText = element.Text?.ToLower().Replace("\t", " ");
                return rowText?.Contains(query2) ?? false;
            }
        ).ToList();

        Log.Information(
            "Found about {FilteredCount} of {AllCount} subtitle with title: '{QueryValue}'",
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
                        id: StringUtil.NewGuid(),
                        title: "Tidak di temukan hasil, silakan cari bahasa/judul yang lain",
                        inputMessageContent: new InputTextMessageContent("Tekan tombol dibawah ini untuk memulai pencarian")
                    )
                    {
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                            }
                        )
                    }
                }
            );
            executionResult.IsSuccess = false;

            return executionResult;
        }

        var urlStart = await telegramService.GetUrlStart("");

        var result = filteredSearch.Select(
            element => {
                var title = element.Text.RegexReplace(@"\t|\n", " ").RegexReplace(@"\s+", " ").Trim();
                var titleParted = title.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var languageSub = titleParted.FirstOrDefault("Language");
                var movieName = titleParted.Skip(1).JoinStr(" ").HtmlEncode();
                var slug = element.PathName.Split("/").Skip(2).JoinStr("/");
                Log.Debug(
                    "Appending Movie with slug: '{0}' => {1}",
                    slug,
                    title
                );

                var content = HtmlMessage.Empty
                    .Bold("Name: ").TextBr(movieName)
                    .Bold("Language: ").TextBr(languageSub)
                    .Bold("Url: ").Url($"https://subscene.com{element.PathName}", "Subscene Link");

                var startDownloadUrl = urlStart + "start=sub-dl_" + slug.Replace("/", "=");

                var article = new InlineQueryResultArticle(
                    id: StringUtil.NewGuid(),
                    title: titleParted.FirstOrDefault("Language"),
                    inputMessageContent: new InputTextMessageContent(content.ToString())
                    {
                        ParseMode = ParseMode.Html,
                        DisableWebPagePreview = true
                    }
                )
                {
                    Description = movieName,
                    ReplyMarkup = new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Ulang pencarian", $"subscene-dl {query1} "),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("Mulai unduh file", startDownloadUrl)
                            }
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
