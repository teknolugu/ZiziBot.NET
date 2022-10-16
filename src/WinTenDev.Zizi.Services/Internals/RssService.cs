using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Humanizer;
using MongoDB.Entities;
using MoreLinq;
using Serilog;
using SqlKata.Execution;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Services.Internals;

public class RssService
{
    private readonly IMapper _mapper;
    private readonly QueryService _queryService;
    private const string CacheKey = "rss-list";
    private const string RSSHistoryTable = "rss_history";
    private const string RSSSettingTable = "rss_settings";

    public RssService(
        IMapper mapper,
        QueryService queryService
    )
    {
        _mapper = mapper;
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

        Log.Debug(
            "Check RSS History exist on ChatId {ChatId}? {IsExist}",
            chatId,
            isExist
        );

        return isExist;
    }

    public async Task<bool> IsRssExist(
        long chatId,
        string urlFeed
    )
    {
        // var data = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(RSSSettingTable)
        //     .Where("chat_id", chatId)
        //     .Where("url_feed", urlFeed)
        //     .GetAsync<RssHistory>();
        //
        // var isExist = data.Any();

        var isExist = await DB.Find<RssSourceEntity>()
            .Match(entity =>
                entity.ChatId == chatId &&
                entity.UrlFeed == urlFeed
            )
            .ExecuteAnyAsync();

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

    public async Task<bool> SaveRssSettingAsync(RssSourceDto rssSourceDto)
    {
        var data = _mapper.Map<RssSourceEntity>(rssSourceDto);

        await data.InsertAsync();

        return true;
    }

    public async Task ImportRssSettingAsync(IEnumerable<RssSourceDto> rssSourceDto)
    {
        var rssSourceEntities = _mapper.Map<IEnumerable<RssSourceEntity>>(rssSourceDto);

        var insert = await rssSourceEntities.InsertAsync();
    }

    public async Task<bool> UpdateRssSettingAsync(
        RssSettingFindDto rssSettingFindDto,
        RssSetting rssSetting
    )
    {
        var where = rssSettingFindDto.ToDictionary(skipZeroNullOrEmpty: true);
        var data = rssSetting.ToDictionary(skipZeroNullOrEmpty: true);

        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where(where)
            .UpdateAsync(data);

        return insert.ToBool();
    }

    public async Task<bool> UpdateRssSettingAsync(
        RssSettingFindDto rssSettingFindDto,
        Dictionary<string, object> data
    )
    {
        var where = rssSettingFindDto.ToDictionary(skipZeroNullOrEmpty: true);

        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where(where)
            .UpdateAsync(data);

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

    public async Task<List<RssSourceEntity>> GetRssSettingsAsync(long chatId)
    {
        // var data = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(RSSSettingTable)
        //     .Where("chat_id", chatId)
        //     .OrderBy("url_feed")
        //     .GetAsync<RssSetting>();

        var data = await DB.Find<RssSourceEntity>()
            .Match(entity => entity.ChatId == chatId)
            .ExecuteAsync();

        Log.Verbose("RSSData: {@V}", data);

        return data;
    }

    public async Task<List<RssSourceEntity>> GetAllRssSettingsAsync()
    {
        // var data = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(RSSSettingTable)
        //     .GetAsync<RssSetting>();

        var data = await DB.Find<RssSourceEntity>()
            .ExecuteAsync();

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
        // var delete = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(RSSSettingTable)
        //     .Where("chat_id", chatId)
        //     .Where("url_feed", urlFeed)
        //     .DeleteAsync();

        var delete = await DB.DeleteAsync<RssSourceEntity>(entity =>
            entity.ChatId == chatId &&
            entity.UrlFeed == urlFeed
        );

        Log.Information(
            "Delete {UrlFeed} status: {V}",
            urlFeed,
            delete
        );

        return delete.ToBool();
    }

    public async Task<bool> DeleteRssAsync(
        long chatId,
        string columnName,
        object columnValue
    )
    {
        var delete = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .Where(columnName, columnValue)
            .DeleteAsync();

        Log.Information(
            "Delete RSS Settings for ChatId {ChatId}. {ColumnName} => {ColumnValue} status: {V}",
            chatId,
            columnName,
            columnValue,
            delete
        );

        return delete.ToBool();
    }

    public async Task<int> DeleteAllByChatId(long chatId)
    {
        var delete = await _queryService
            .CreateMySqlFactory()
            .FromTable(RSSSettingTable)
            .Where("chat_id", chatId)
            .DeleteAsync();

        Log.Information(
            "Deleted RSS {ChatId} Settings {Delete} rows",
            chatId,
            delete
        );

        return delete;
    }

    [JobDisplayName("Delete olds RSS History")]
    public async Task DeleteOldHistory()
    {
        var dateTime = DateTime.UtcNow.AddMonths(-6);

        // var delete = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(tableName: RSSHistoryTable)
        //     .Where(
        //         column: "created_at",
        //         op: "<",
        //         value: dateTime
        //     )
        //     .DeleteAsync();

        var delete = await DB.DeleteAsync<ArticleSentEntity>(sent => sent.CreatedOn < dateTime);

        var rowsItems = "row".ToQuantity(delete.DeletedCount);

        Log.Information("About {RowItems} RSS History deleted", rowsItems);
    }

    public async Task<int> DeleteDuplicateAsync()
    {
        var query = await _queryService.CreateMySqlFactory()
            .DeleteDuplicateAsync(
                RSSSettingTable,
                "id",
                "chat_id",
                "url_feed"
            );

        return query;
    }

    public async Task<InlineKeyboardMarkup> GetButtonMarkup(
        long chatId,
        int page = 0,
        int take = 5
    )
    {
        var buttonMarkup = InlineKeyboardMarkup.Empty();
        var rssSettings = await GetRssSettingsAsync(chatId);
        var filtered = rssSettings.Skip(page * take).Take(take);

        var prev = page - take;
        var next = page + take;

        var buttons = new List<InlineKeyboardButton[]>();

        if (filtered.Any())
        {
            buttons.Add(
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("🚫 Stop All", $"rssctl stop-all"),
                    InlineKeyboardButton.WithCallbackData("✅ Start All", $"rssctl start-all"),
                }
            );

            filtered.ForEach(
                (
                    rssSetting,
                    index
                ) => {
                    var btnCtl = new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData("❌ Delete", $"rssctl delete {rssSetting.ID}"),
                    };

                    if (rssSetting.IsEnabled)
                        btnCtl.Add(InlineKeyboardButton.WithCallbackData("✅ Started", $"rssctl stop {rssSetting.ID}"));
                    else
                        btnCtl.Add(InlineKeyboardButton.WithCallbackData("🚫 Stopped", $"rssctl start {rssSetting.ID}"));

                    // if (rssSetting.UrlFeed.IsGithubReleaseUrl())
                    //     if (rssSetting.IncludeAttachment)
                    //         btnCtl.Add(InlineKeyboardButton.WithCallbackData("✅ Attachment", $"rssctl attachment-off {rssSetting.Id}"));
                    //     else
                    //         btnCtl.Add(InlineKeyboardButton.WithCallbackData("❌ Attachment", $"rssctl attachment-on {rssSetting.Id}"));

                    buttons.AddRange(
                        new InlineKeyboardButton[][]
                        {
                            new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithUrl($"{index + 1}. {rssSetting.UrlFeed}", rssSetting.UrlFeed),
                            },
                            btnCtl.ToArray()
                        }
                    );
                }
            );

            buttons.Add(
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Prev", $"rssctl navigate-page {page - 1}"),
                    InlineKeyboardButton.WithCallbackData("Next", $"rssctl navigate-page {page + 1}"),
                }
            );

            buttonMarkup = new InlineKeyboardMarkup(buttons);
        }

        return buttonMarkup;
    }

}