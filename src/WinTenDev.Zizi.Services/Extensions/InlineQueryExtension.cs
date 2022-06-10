using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Entities.MongoDb;
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

        var learnMoreContent = $"Silakan pelajari selengkapnya" +
                               $"\n{learnMore}" +
                               $"\n\nAtau tekan salah satu tombol dibawah ini";

        var replyMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Ping", $"ping")
                },
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Buat pesan dengan tombol", $"message")
                },
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Cari subtitle", "subscene ")
                }
            }
        );

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                new InlineQueryResultArticle(
                    id: "guide-1",
                    title: "Bagaimana cara menggunakannya?",
                    inputMessageContent: new InputTextMessageContent(learnMoreContent)
                    {
                        DisableWebPagePreview = true
                    }
                )
                {
                    ReplyMarkup = replyMarkup
                },
                new InlineQueryResultArticle(
                    id: "guide-2",
                    title: "Cobalah ketikkan 'ping'",
                    inputMessageContent: new InputTextMessageContent(learnMoreContent)
                    {
                        DisableWebPagePreview = true
                    }
                )
                {
                    ReplyMarkup = replyMarkup
                }
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
            var learnMore = "Pelajari cara membuat Pesan dengan Tombol via InlineQuery";
            var urlArticle = "https://docs.zizibot.winten.my.id/features/inline-query/pesan-dengan-tombol";

            await telegramService.AnswerInlineQueryAsync(
                new List<InlineQueryResult>()
                {
                    new InlineQueryResultArticle(
                        "iq-learn-mode",
                        "Pesan dengan tombol via InlineQuery",
                        new InputTextMessageContent(learnMore)
                        {
                            DisableWebPagePreview = true
                        }
                    )
                    {
                        Description = learnMore,
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai membuat", "message ")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithUrl("Pelajari selengkapnya..", urlArticle)
                                }
                            }
                        )
                    }
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
        List<SubsceneMovieSearch> subsceneMovieSearches;

        var queryCmd = telegramService.InlineQueryCmd;
        var queryValue = telegramService.InlineQueryValue;
        Log.Information("Starting find Subtitle with title: '{QueryValue}'", queryValue);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();

        if (queryValue.IsNotNullOrEmpty())
        {
            subsceneMovieSearches = await subsceneService.GetOrFeedMovieByTitle(queryValue);

            if (subsceneMovieSearches == null)
            {
                var title = "Tidak di temukan hasil, silakan cari judul yang lain";
                if (queryValue.IsNullOrEmpty())
                {
                    title = "Silakan masukkan judul yang ingin dicari";
                }

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
        }
        else
        {
            subsceneMovieSearches = await subsceneService.GetOrFeedMovieByTitle("");
        }

        Log.Information(
            "Found about {AllCount} title with query: '{QueryValue}'",
            subsceneMovieSearches.Count,
            queryValue
        );

        var inlineQueryResultArticles = subsceneMovieSearches
            .OrderByDescending(search => search.CreatedOn)
            .Select(
                item => {
                    var movieTitle = item.MovieName;
                    var pathName = item.MovieUrl;
                    var subtitleCount = item.SubtitleCount;
                    var moviePath = pathName.Split("/").Take(3).JoinStr("/");
                    var slug = pathName.Split("/").ElementAtOrDefault(2);
                    var subsceneUrl = $"https://subscene.com{moviePath}";

                    var titleHtml = HtmlMessage.Empty
                        .Bold("Judul: ").CodeBr(movieTitle)
                        .Bold("Tersedia : ").CodeBr(subtitleCount);

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
                        Description = $"Tersedia: {subtitleCount}",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithUrl("Tautan Subscene", subsceneUrl)
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian lanjut", $"subscene {queryValue} "),
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai unduh", $"subscene-dl {slug} "),
                                }
                            }
                        )
                    };

                    return article;
                }
            );

        // }
        await telegramService.AnswerInlineQueryAsync(inlineQueryResultArticles);
        executionResult.IsSuccess = true;

        return executionResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQuerySubsceneDownloadAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();
        var queryCmd = telegramService.GetInlineQueryAt<string>(0);
        var query1 = telegramService.GetInlineQueryAt<string>(1);
        var query2 = telegramService.GetInlineQueryAt<string>(2);
        var queryValue = telegramService.InlineQueryValue;
        Log.Information("Starting find Subtitle file with title: '{QueryValue}'", query1);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();
        var searchBySlug = await subsceneService.GetOrFeedSubtitleBySlug(query1);
        Log.Information(
            "Found about {AllCount} subtitle by slug: '{QueryValue}'",
            searchBySlug.Count,
            query1
        );

        var filteredSearch = searchBySlug.Where(
            element => {
                if (query2.IsNullOrEmpty()) return true;

                return element.Language.Contains(query2, StringComparison.CurrentCultureIgnoreCase) ||
                       element.MovieName.Contains(query2, StringComparison.CurrentCultureIgnoreCase) ||
                       element.Owner.Contains(query2, StringComparison.CurrentCultureIgnoreCase);
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
                var languageSub = element.Language;
                var movieName = element.MovieName;
                var movieUrl = element.MovieUrl;
                var ownerSub = element.Owner;
                var slug = element.MovieUrl?.Split("/").Skip(2).JoinStr("/");
                var subtitleUrl = "https://subscene.com" + movieUrl;

                Log.Debug(
                    "Appending Movie with slug: '{0}' => {1}",
                    slug,
                    movieName
                );

                var titleResult = $"{languageSub} | {ownerSub}";

                var content = HtmlMessage.Empty
                    .Bold("Nama/Judul: ").CodeBr(movieName)
                    .Bold("Bahasa: ").CodeBr(languageSub)
                    .Bold("Pemilik: ").Text(element.Owner);

                var startDownloadUrl = urlStart + "start=sub-dl_" + slug.Replace("/", "=");

                var article = new InlineQueryResultArticle(
                    id: StringUtil.NewGuid(),
                    title: titleResult,
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
                                InlineKeyboardButton.WithUrl("Tautan subtitle", subtitleUrl)
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian lanjut", $"subscene-dl {queryValue} "),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Ulang pencarian", $"subscene-dl {query1} "),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("Unduh subtitle", startDownloadUrl),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
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
