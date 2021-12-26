using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Hangfire;
using Serilog;
using WinTenDev.Zizi.Utils.Text;

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
        Log.Information("Scanning {0} ..", url);
        var urls = await FeedReader.GetFeedUrlsFromUrlAsync(url);
        Log.Debug("UrlFeeds: {0}", urls.ToJson());

        var feedUrl = "";
        var urlCount = urls.Count();

        if (urlCount == 1)// no url - probably the url is already the right feed url
            feedUrl = url;
        else if (urlCount == 1)
            feedUrl = urls.First().Url;
        else if (urlCount == 2
                )// if 2 urls, then its usually a feed and a comments feed, so take the first per default
            feedUrl = urls.First().Url;

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
            Log.Error(ex.Demystify(), "Validating RSS Feed");
        }

        Log.Debug("{0} IsValidUrlFeed: {1}", url, isValid);

        return isValid;
    }
}