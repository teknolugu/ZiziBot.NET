using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Humanizer;
using Serilog;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Internals;

public class RssService
{
    private readonly QueryService _queryService;
    private const string CacheKey = "rss-list";
    private const string RSSHistoryTable = "rss_history";
    private const string RSSSettingTable = "rss_settings";

    public RssService(
        QueryService queryService
    )
    {
        _queryService = queryService;
    }

    public async Task<bool> IsHistoryExist(
        long chatId,
        string url
    )
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSHistoryTable)
            .Where("chat_id", chatId)
            .Where("url", url)
            .GetAsync<RssHistory>();

        var isExist = data.Any();

        Log.Debug
        (
            "Check RSS History exist on ChatId {ChatId}? {IsExist}",
            chatId, isExist
        );

        return isExist;
    }

    public async Task<bool> IsRssExist(
        long chatId,
        string urlFeed
    )
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .Where("url_feed", urlFeed)
            .GetAsync<RssHistory>();

        var isExist = data.Any();
        Log.Information("Check RSS Setting: {IsExist}", isExist);

        return isExist;
    }

    public string GetCacheKey(long chatId)
    {
        var reduced = chatId.ReduceChatId();
        return $"{CacheKey}_{reduced}";
    }

    public async Task<bool> SaveRssSettingAsync(Dictionary<string, object> data)
    {
        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .InsertAsync(data);

        return insert.ToBool();
    }

    public async Task<int> SaveRssHistoryAsync(RssHistory rssHistory)
    {
        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSHistoryTable)
            .InsertAsync(rssHistory);

        return insert;
    }

    public async Task<IEnumerable<RssSetting>> GetRssSettingsAsync(long chatId)
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .GetAsync<RssSetting>();

        Log.Verbose("RSSData: {@V}", data);

        return data;
    }

    public async Task<IEnumerable<RssSetting>> GetAllRssSettingsAsync()
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .GetAsync<RssSetting>();

        Log.Verbose("RSSData: {@Data}", data);

        return data;
    }

    public async Task<IEnumerable<RssHistory>> GetRssHistory(RssHistory rssHistory)
    {
        var where = new Dictionary<string, object>()
        {
            ["chat_id"] = rssHistory.ChatId,
            ["rss_source"] = rssHistory.RssSource
        };

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSHistoryTable)
            .Where(where)
            .GetAsync<RssHistory>();

        return query;
    }

    public async Task<bool> DeleteRssAsync(
        long chatId,
        string urlFeed
    )
    {
        var delete = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .Where("url_feed", urlFeed)
            .DeleteAsync();

        Log.Information("Delete {UrlFeed} status: {V}", urlFeed, delete);

        return delete.ToBool();
    }

    public async Task<int> DeleteAllByChatId(long chatId)
    {
        var delete = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .DeleteAsync();

        Log.Information("Deleted RSS {ChatId} Settings {Delete} rows", chatId, delete);

        return delete;
    }

    [JobDisplayName("Delete olds RSS History")]
    public async Task DeleteOldHistory()
    {
        var delete = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSHistoryTable)
            .WhereDate("created_at", "<", DateTime.Now.AddMonths(-6))
            .DeleteAsync();

        var rowsItems = "row".ToQuantity(delete);

        Log.Information("About {RowItems} RSS History deleted", rowsItems);
    }

    public async Task<int> DeleteDuplicateAsync()
    {
        var query = await _queryService.CreateMySqlFactory()
            .DeleteDuplicateAsync(RSSSettingTable, "id", "chat_id", "url_feed");

        return query;
    }
}