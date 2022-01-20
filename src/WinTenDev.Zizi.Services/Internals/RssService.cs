using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

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

    public async Task<bool> IsHistoryExist(RssHistory rssHistory)
    {
        var where = new Dictionary<string, object>()
        {
            { "ChatId", rssHistory.ChatId },
            { "Url", rssHistory.Url }
        };

        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSHistoryTable)
            .Where(where)
            .GetAsync();

        var isExist = data.Any();
        Log.Debug("Check RSS History: {IsExist}", isExist);

        return isExist;
    }

    public async Task<bool> IsExistRssAsync(
        long chatId,
        string urlFeed
    )
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .Where("url_feed", urlFeed)
            .GetAsync();

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

    public async Task<List<RssSetting>> GetRssSettingsAsync(long chatId)
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .GetAsync();

        var mapped = data.ToJson().MapObject<List<RssSetting>>();
        Log.Debug("RSSData: {@V}", mapped);

        return mapped;
    }

    public async Task<IEnumerable<RssSetting>> GetAllRssSettingsAsync()
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .GetAsync<RssSetting>();

        Log.Debug("RSSData: {@Data}", data);

        return data;
    }

    public async Task<List<RssHistory>> GetRssHistory(RssHistory rssHistory)
    {
        var where = new Dictionary<string, object>()
        {
            ["ChatId"] = rssHistory.ChatId,
            ["RssSource"] = rssHistory.RssSource
        };

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSHistoryTable)
            .Where(where)
            .GetAsync();

        return query.ToJson().MapObject<List<RssHistory>>();
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

    public async Task DeleteDuplicateAsync()
    {
        await Task.CompletedTask;
    }
}