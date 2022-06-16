using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
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
            Log.Error(
                ex.Demystify(),
                "Validating RSS Feed. Url: {Url}",
                url
            );
        }

        Log.Debug(
            "{0} IsValidUrlFeed: {1}",
            url,
            isValid
        );

        return isValid;
    }

    public static async Task<SyndicationFeed> OpenSyndicationFeed(string url)
    {
        Log.Information("Opening SyndicationFeed: {Url} ..", url);
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "ZiziBot RSS Reader/1.0");
        var stream = await httpClient.GetStreamAsync(url);
        var feed = SyndicationFeed.Load(XmlReader.Create(stream));

        Log.Debug("SyndicationFeed count {@Feed} item(s)", feed.Items.Count());

        return feed;
    }
}
