using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Hangfire;
using Serilog;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Utils;

public static class RssFeedUtil
{
    public static void UnRegRSS(long chatId)
    {
        var baseId = "rss";
        var reduceChatId = chatId.ToInt64().ReduceChatId();
        var recurringId = $"{baseId}-{reduceChatId}";

        Log.Debug("Deleting RSS Cron {0}", chatId);
        RecurringJob.RemoveIfExists(recurringId);
    }

    public static async Task<string> FindUrlFeed(this string url)
    {
        Log.Information("Scanning {Url} ..", url);
        var urls = await FeedReader.GetFeedUrlsFromUrlAsync(url);
        Log.Debug("UrlFeeds: {@URLs}", urls);

        var feedUrl = urls.FirstOrDefault()?.Url;

        return feedUrl;
    }

    public static async Task<bool> IsValidUrlFeed(this string url)
    {
        var isValid = false;

        try
        {
            var feed = await FeedReader.ReadAsync(url);
            isValid = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Demystify(), "Validating RSS Feed. Url: {Url}", url);
        }

        Log.Debug("{0} IsValidUrlFeed: {1}", url, isValid);

        return isValid;
    }
}