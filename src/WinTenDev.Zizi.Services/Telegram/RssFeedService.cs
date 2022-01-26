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
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Services.Telegram;

public class RssFeedService
{
    private readonly RssService _rssService;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly TelegramBotClient _botClient;

    public RssFeedService(
        IRecurringJobManager recurringJobManager,
        TelegramBotClient botClient,
        RssService rssService
    )
    {
        _recurringJobManager = recurringJobManager;
        _botClient = botClient;
        _rssService = rssService;
    }

    public async Task RegisterJobAllRssScheduler()
    {
        Log.Information("Initializing RSS Scheduler..");

        Log.Information("Getting list Chat ID");
        var listChatId = await _rssService.GetAllRssSettingsAsync();

        foreach (var rssSetting in listChatId)
        {
            var chatId = rssSetting.ChatId;
            var urlFeed = rssSetting.UrlFeed;

            RegisterUrlFeed(chatId, urlFeed);
        }

        Log.Information("Registering RSS Scheduler complete..");
    }

    public void RegisterUrlFeed(
        long chatId,
        string urlFeed
    )
    {
        var reducedChatId = chatId.ReduceChatId();
        var unique = StringUtil.GenerateUniqueId(3);
        var recurringId = $"RSS_{reducedChatId}_{unique}";

        Log.Debug("Registering URl: {UrlFeed} for ChatId: {ChatId} with JobId: {RecurringId}",
            urlFeed, chatId, recurringId);

        _recurringJobManager.AddOrUpdate<RssFeedService>(recurringId, service =>
            service.ExecuteUrlAsync(chatId, urlFeed), Cron.Minutely);
    }

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    [JobDisplayName("RSS {0}")]
    public async Task ExecuteUrlAsync(
        long chatId,
        string rssUrl
    )
    {
        Log.Information("Reading feed from {ChatId}. Url: {RssUrl}", chatId, rssUrl);
        var rssFeeds = await FeedReader.ReadAsync(rssUrl);

        var rssTitle = rssFeeds.Title;
        var rssFeed = rssFeeds.Items.FirstOrDefault();

        Log.Debug("Getting last history for {ChatId} url {RssUrl}", chatId, rssUrl);

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
            Log.Information("Last article from feed '{RssUrl}' has sent to {ChatId}", rssUrl, chatId);
        }
        else
        {
            Log.Information("Sending article from feed {RssUrl} to {ChatId}", rssUrl, chatId);

            try
            {
                await _botClient.SendTextMessageAsync(chatId, sendText, ParseMode.Html);

                Log.Debug("Writing to RSS History");

                await _rssService.SaveRssHistoryAsync(new RssHistory()
                {
                    Url = rssFeed.Link,
                    RssSource = rssUrl,
                    ChatId = chatId,
                    Title = rssFeed.Title,
                    PublishDate = rssFeed.PublishingDate ?? DateTime.Now,
                    Author = rssFeed.Author ?? "N/A",
                    CreatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "RSS Broadcaster Error at ChatId {ChatId}. Url: {Url}", chatId, rssUrl);
                var exMessage = ex.Message;
                if (exMessage.Contains("bot was blocked by the user"))
                {
                    Log.Warning("Seem need clearing all RSS Settings and unregister Cron completely!");
                    Log.Debug("Deleting all RSS Settings");
                    await _rssService.DeleteAllByChatId(chatId);

                    UnRegisterRssFeedByChatId(chatId);
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
        var filteredJobs = recurringJobs.Where(job =>
            job.Id.Contains(prefixJobId));

        filteredJobs.ForEach(job => {
            Log.Debug("Remove RSS Cron With ID {JobId}. Args: {Args}", job.Id, job.Job.Args);
            _recurringJobManager.RemoveIfExists(job.Id);
        });

        op.Complete();

        return filteredJobs.Count();
    }

    public async Task RegisterRssFeedByChatId(long chatId)
    {
        var op = Operation.Begin("Registering RSS by ChatId: {ChatId}", chatId);

        var rssSettings = await _rssService.GetRssSettingsAsync(chatId);
        rssSettings.ForEach(setting => {
            RegisterUrlFeed(chatId, setting.UrlFeed);
        });

        op.Complete();
    }

    public async Task ReRegisterRssFeedByChatId(long chatId)
    {
        UnRegisterRssFeedByChatId(chatId);
        await RegisterRssFeedByChatId(chatId);
    }
}