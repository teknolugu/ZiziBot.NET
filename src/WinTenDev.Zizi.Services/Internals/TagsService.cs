using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class TagsService
{
    private const string TableName = "tags";
    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;

    public TagsService(
        QueryService queryService,
        CacheService cacheService
    )
    {
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public async Task<bool> IsExist(
        long chatId,
        string tagVal
    )
    {
        var data = await GetTagByTag(chatId, tagVal);
        var isExist = data.Any();
        Log.Debug(
            "Tag in '{ChatId}' with slug '{TagVal} is exist? {IsExist}",
            chatId,
            tagVal,
            isExist
        );

        return isExist;
    }

    public string GetCacheKey(long chatId)
    {
        var reducedChatId = chatId.ReduceChatId();
        return $"tags-{reducedChatId}";
    }

    public async Task<IEnumerable<CloudTag>> GetTagsByGroupAsync(long chatId)
    {
        var key = GetCacheKey(chatId);
        var data = await _cacheService.GetOrSetAsync(
            key,
            () =>
                GetTagsByGroupCoreAsync(chatId)
        );

        return data;
    }

    public async Task<IEnumerable<CloudTag>> GetTagsByGroupCoreAsync(long chatId)
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where("chat_id", chatId)
            .OrderBy("tag")
            .GetAsync<CloudTag>();

        return data.Where(tag => tag.Tag.WordsCount() == 1);
    }

    public async Task<IEnumerable<CloudTag>> GetTagByTag(
        long chatId,
        string tag
    )
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where("chat_id", chatId)
            .Where("tag", tag)
            .OrderBy("tag")
            .GetAsync<CloudTag>();

        Log.Debug(
            "Tag by Tag for {ChatId} => {@V}",
            chatId,
            data
        );
        return data;
    }

    public async Task SaveTagAsync(Dictionary<string, object> data)
    {
        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(data);

        Log.Information("SaveTag: {Insert}", insert);
    }

    public async Task<int> SaveTagAsync(CloudTag data)
    {
        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(data);

        Log.Information("SaveTag: {Insert}", insert);

        return insert;
    }

    public async Task<bool> DeleteTag(
        long chatId,
        string tag
    )
    {
        var delete = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where("chat_id", chatId)
            .Where("tag", tag)
            .DeleteAsync();

        return delete > 0;
    }

    public async Task UpdateCacheAsync(long chatId)
    {
        var key = GetCacheKey(chatId);
        await _cacheService.EvictAsync(key);
    }
}