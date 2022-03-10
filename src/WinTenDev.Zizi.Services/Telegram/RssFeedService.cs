using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Hangfire;
using Hangfire.Storage;
using MoreLinq;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Parsers;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Telegram;

public class RssFeedService
{
    private readonly RssService _rssService;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ITelegramBotClient _botClient;
    private readonly JobsService _jobsService;

    public RssFeedService(
        IRecurringJobManager recurringJobManager,
        ITelegramBotClient botClient,
        JobsService jobsService,
        RssService rssService
    )
    {
        _recurringJobManager = recurringJobManager;
        _botClient = botClient;
        _jobsService = jobsService;
        _rssService = rssService;
    }

    public async Task RegisterJobAllRssScheduler()
    {
        var op = Operation.Begin("Registering RSS Job");

        Log.Information("Getting list Chat ID");
        var rssSettings = await _rssService.GetAllRssSettingsAsync();
        var listChatId = rssSettings.Select(setting => setting.ChatId).Distinct();

        await listChatId.ForEachAsync(4,
            async chatId => {
                await RegisterRssFeedByChatId(chatId);
            });

        op.Complete();
    }

    public void RegisterUrlFeed(
        long chatId,
        string urlFeed
    )
    {
        var reducedChatId = chatId.ReduceChatId();
        var unique = StringUtil.GenerateUniqueId(3);
        var recurringId = $"RSS_{reducedChatId}_{unique}";

        Log.Debug(
            "Register RSS for ChatId: {ChatId} with JobId: {RecurringId}. URl: {UrlFeed} ",
            chatId.ReduceChatId(),
            recurringId,
            urlFeed
        );

        _recurringJobManager.AddOrUpdate<RssFeedService>(
            recurringId,
            service =>
                service.ExecuteUrlAsync(chatId, urlFeed),
            Cron.Minutely
        );
    }

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    [JobDisplayName("RSS {0}")]
    public async Task ExecuteUrlAsync(
        long chatId,
        string rssUrl
    )
    {
        Log.Information(
            "Reading feed from {ChatId}. Url: {RssUrl}",
            chatId,
            rssUrl
        );

        var isWordpress = await rssUrl.IsWordpress();

        var rssFeeds = await FeedReader.ReadAsync(rssUrl);

        var rssTitle = rssFeeds.Title;
        var rssFeed = rssFeeds.Items.FirstOrDefault();

        Log.Debug(
            "Getting last history for {ChatId} url {RssUrl}",
            chatId,
            rssUrl
        );

        if (rssFeed == null) return;

        Log.Debug("CurrentArticleDate: {Date}", rssFeed.PublishingDate);
        Log.Debug("Prepare sending article to ChatId {ChatId}", chatId);

        // var titleLink = $"{rssTitle} - {rssFeed.Title}".MkUrl(rssFeed.Link);
        var category = rssFeed.Categories.MkJoin(", ");
        var sendText = $"{rssTitle} - {rssFeed.Title}" +
                       $"\n{rssFeed.Link}" +
                       $"\nTags: {category}";

        var isExist = await _rssService.IsHistoryExist(chatId, rssFeed.Link);

        if (isExist)
        {
            Log.Information(
                "Last article from feed '{RssUrl}' has sent to {ChatId}",
                rssUrl,
                chatId
            );
        }
        else
        {
            Log.Information(
                "Sending article from feed {RssUrl} to {ChatId}",
                rssUrl,
                chatId
            );

            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    sendText,
                    ParseMode.Html
                );

                Log.Debug("Writing to RSS History");

                await _rssService.SaveRssHistoryAsync
                (
                    new RssHistory
                    {
                        Url = rssFeed.Link,
                        RssSource = rssUrl,
                        ChatId = chatId,
                        Title = rssFeed.Title,
                        PublishDate = rssFeed.PublishingDate ?? DateTime.Now,
                        Author = rssFeed.Author ?? "N/A",
                        CreatedAt = DateTime.Now
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex.Demystify(),
                    "RSS Broadcaster Error at ChatId {ChatId}. Url: {Url}",
                    chatId,
                    rssUrl
                );

                if (ex.Message.ContainsListStr(
                        "blocked",
                        "not found",
                        "deactivated"
                    ))
                {
                    UnregisterRssFeed(chatId, rssUrl);
                }
            }
        }
    }

    public int UnRegisterRssFeedByChatId(long chatId)
    {
        var op = Operation.Begin("UnRegistering RSS by ChatId: {ChatId}", chatId);

        var reduceChatId = chatId.ReduceChatId();
        var prefixJobId = $"RSS_{reduceChatId}";

        var connection = JobStorage.Current.GetConnection();

        var recurringJobs = connection.GetRecurringJobs();
        var filteredJobs = recurringJobs.Where
        (
            job =>
                job.Id.Contains(prefixJobId)
        );

        filteredJobs.ForEach
        (
            job => {
                Log.Debug(
                    "Remove RSS Cron With ID {JobId}. Args: {Args}",
                    job.Id,
                    job.Job.Args
                );
                _recurringJobManager.RemoveIfExists(job.Id);
            }
        );

        op.Complete();

        return filteredJobs.Count();
    }

    public void UnregisterRssFeed(
        long chatId,
        string urlFeed
    )
    {
        var selectJobs = _jobsService
            .GetRecurringJobs()
            .Find
            (
                dto => {
                    var args = dto.Job.Args;
                    if (args.Count != 2) return false;

                    var argChatId = args.ElementAtOrDefault(0);
                    var argRssUrl = args.ElementAtOrDefault(1);

                    var isMatch = argChatId?.ToInt64() == chatId && argRssUrl?.ToString() == urlFeed;
                    return isMatch;
                }
            );

        if (selectJobs == null) return;

        Log.Debug(
            "Deleting Job: {Job}. Args: {Args} ",
            selectJobs.Id,
            selectJobs.Job.Args
        );
        _recurringJobManager.RemoveIfExists(selectJobs.Id);
    }

    public async Task RegisterRssFeedByChatId(long chatId)
    {
        var op = Operation.Begin("Registering RSS by ChatId: {ChatId}", chatId);

        var rssSettings = await _rssService.GetRssSettingsAsync(chatId);
        var filteredSettings = rssSettings.Where(
            (
                setting,
                index
            ) => setting.IsEnabled
        );

        filteredSettings.ForEach
        (
            setting => {
                RegisterUrlFeed(chatId, setting.UrlFeed);
            }
        );

        op.Complete();
    }

    public async Task ReRegisterRssFeedByChatId(long chatId)
    {
        UnRegisterRssFeedByChatId(chatId);
        await RegisterRssFeedByChatId(chatId);
    }
}