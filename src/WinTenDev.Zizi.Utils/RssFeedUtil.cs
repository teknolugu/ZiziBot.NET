using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using CodeHollow.FeedReader;
using Flurl.Http;
using Hangfire;
using Serilog;
using WinTenDev.Zizi.Utils.Parsers;
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
            var feed = await OpenSyndicationFeed(url);
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

    public static async Task<SyndicationFeed> OpenSyndicationFeed(this string url)
    {
        Log.Information("Opening SyndicationFeed: {Url} ..", url);
        var stream = await url.OpenFlurlSession().GetStreamAsync();
        var feed = SyndicationFeed.Load(XmlReader.Create(stream));

        Log.Debug("SyndicationFeed count {@Feed} item(s)", feed.Items.Count());

        return feed;
    }

    public static SyndicationFeed OpenSyndicationFeedFromString(this string rssContent)
    {
        Log.Information("Opening SyndicationFeed from String");
        var feed = SyndicationFeed.Load(XmlReader.Create(rssContent.ToStream()));

        Log.Debug("SyndicationFeed count {@Feed} item(s)", feed.Items.Count());

        return feed;
    }

    public static string TryFixRssUrl(this string rssUrl)
    {
        var fixedUrl = rssUrl;

        if (rssUrl.EndsWith("feed"))
            fixedUrl = rssUrl + "/";

        if ((rssUrl.IsGithubReleaseUrl() || rssUrl.IsGithubCommitsUrl()) &&
            !rssUrl.EndsWith(".atom")) fixedUrl = rssUrl + ".atom";

        Log.Debug(
            "Try fix Rss URL: {Url}. After fix: {FixedUrl}",
            rssUrl,
            fixedUrl
        );

        return fixedUrl;
    }
}