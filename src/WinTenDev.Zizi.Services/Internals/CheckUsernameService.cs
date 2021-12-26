using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyCaching.Core;
using Serilog;
using SerilogTimings;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals;

public class CheckUsernameService
{
    private const string TableName = "warn_username_history";

    private readonly QueryService _queryService;
    private readonly SettingsService _settingsService;
    private readonly IEasyCachingProvider _cachingProvider;

    /// <summary>
    /// Username service constructor
    /// </summary>
    /// <param name="queryService"></param>
    /// <param name="settingsService"></param>
    /// <param name="cachingProvider"></param>
    public CheckUsernameService(
        QueryService queryService,
        SettingsService settingsService,
        IEasyCachingProvider cachingProvider
    )
    {
        _queryService = queryService;
        _settingsService = settingsService;
        _cachingProvider = cachingProvider;
    }

    public async Task<bool> IsExist(long chatId, long fromId)
    {
        var history = await GetHistoryCore(chatId, fromId);
        var isExist = history != null;
        Log.Debug("Warn Username History UserID: '{FromId}' is Exist? '{IsExist}'", fromId, isExist);

        return isExist;
    }

    public string GetCacheKey(long chatId, long userId)
    {
        var redChatId = chatId.ReduceChatId();
        var key = TableName.Replace("_", "-") + $"_{redChatId}_{userId}";
        return key;
    }

    /// <summary>
    /// Get History username by userId (uncached)
    /// </summary>
    /// <returns></returns>
    public async Task<WarnUsernameHistory> GetHistoryCore(long chatId, long fromId)
    {
        var factory = await _queryService
            .CreateMySqlConnection()
            .FromTable(TableName)
            .Where("chat_id", chatId)
            .Where("from_id", fromId)
            .FirstOrDefaultAsync<WarnUsernameHistory>();

        Log.Debug("Get warn Username by UserID: {FromId}", fromId);

        return factory;
    }

    public async Task<WarnUsernameHistory> GetHistory(long chatId, long userId)
    {
        var key = GetCacheKey(chatId, userId);
        var isCached = await _cachingProvider.ExistsAsync(key);
        if (!isCached)
        {
            await UpdateWarnByIdCacheAsync(chatId, userId);
        }

        var cache = await _cachingProvider.GetAsync<WarnUsernameHistory>(key);
        return cache.Value;
    }

    public async Task UpdateWarnByIdCacheAsync(long chatId, long userId)
    {
        var key = GetCacheKey(chatId, userId);
        var data = await GetHistoryCore(chatId, userId);

        if (data == null) return;

        await _cachingProvider.SetAsync(key, data, TimeUtil.YearSpan(30));
    }

    public async Task SaveUsername(WarnUsernameHistory history)
    {
        var fromId = history.FromId;
        var chatId = history.ChatId;

        var query = _queryService
            .CreateMySqlConnection()
            .FromTable(TableName);

        history.UpdatedAt = DateTime.Now;

        var isExist = await IsExist(chatId, fromId);
        if (isExist)
        {
            Log.Debug("Updating warn Username UserID: '{FromId}'", fromId);
            history.StepCount++;

            await query
                .Where("from_id", fromId)
                .UpdateAsync(history);
        }
        else
        {
            Log.Debug("Inserting warn Username UserID: '{FromId}'", fromId);
            history.StepCount = 1;

            await query.InsertAsync(history);
        }

        await UpdateWarnByIdCacheAsync(chatId, fromId);

        Log.Debug("Save warn Username by UserID: {FromId} completed", fromId);
    }

    public async Task<int> UpdateLastMessageId(long chatId, long fromId, long messageId)
    {
        var data = new Dictionary<string, object>()
        {
            { "last_warn_message_id", messageId }
        };

        var query = await _queryService.CreateMySqlConnection()
            .FromTable(TableName)
            .Where("chat_id", chatId)
            .Where("from_id", fromId)
            .UpdateAsync(data);

        await UpdateWarnByIdCacheAsync(chatId, fromId);

        return query;
    }

    /// <summary>Resets the warn username.</summary>
    /// <param name="fromId">From identifier.</param>
    /// <returns>
    ///   <br />
    /// </returns>
    public async Task<int> ResetWarnUsername(long fromId)
    {
        using var op = Operation.Begin($"Starting delete Warn Username by UserID: '{fromId}'");
        var query = await _queryService
            .CreateMySqlConnection()
            .FromTable(TableName)
            .Where("from_id", fromId)
            .DeleteAsync();

        Log.Debug("Delete warn Username by UserID: {Query}", query);

        op.Complete();

        return query;
    }

    public async Task RemoveCache(long chatId, long userId)
    {
        var cacheKey = GetCacheKey(chatId, userId);

        Log.Debug("Deleting cache UserID: '{0}' from ChatID: '{1}'", userId, chatId);

        // if (await _cachingProvider.ExistsAsync(cacheKey))
        await _cachingProvider.RemoveAsync(cacheKey);
    }

    public async Task RemoveAll(long chatId, long userId)
    {
        var prefixKey = TableName.Replace("_", "-");

        var caches = await _cachingProvider.GetByPrefixAsync<WarnUsernameHistory>(prefixKey);
        var filtered = caches
            .Where((pair, index) => pair.Key.Contains(userId.ToString()))
            .ToList();

        if (!filtered.Any())
        {
            Log.Debug("No Warn Username cache by UserID: '{0}' for deleting..", userId);
            return;
        }

        foreach (var cacheValue in filtered)
        {
            Log.Debug("Deleting caches for {0} with cacheKey {1}", userId, cacheValue.Key);
            await _cachingProvider.RemoveAsync(cacheValue.Key);
        }
    }
}