using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Entities;
using Serilog;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class GlobalBanService
{
    private const string GBanTable = "global_bans";
    private const string GBanAdminTable = "gban_admin";
    private const string CacheKey = "global-bans";

    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;

    public GlobalBanService(
        QueryService queryService,
        CacheService cacheService
    )
    {
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public async Task<bool> IsExist(long userId)
    {
        // var query = await GetGlobalBanByIdCore(userId);

        // var isBan = query != null;

        var isBan = await DB.Find<GlobalBanUserEntity>()
            .Match(entity => entity.BannedUserId == userId)
            .ExecuteAnyAsync();

        Log.Information(
            "UserId '{UserId}' Is ES2 Ban? {IsBan}",
            userId,
            isBan
        );

        return isBan;
    }

    public string GetCacheKey(long userId)
    {
        return $"ban-es2_{userId}";
    }

    /// <summary>
    /// Saves the ban async.
    /// </summary>
    /// <param name="globalBanItem">The global ban data.</param>
    /// <returns>A Task.</returns>
    public async Task<bool> SaveBanAsync(GlobalBanItem globalBanItem)
    {
        // var userId = globalBanItem.UserId;
        // var fromId = globalBanItem.BannedBy;
        // var chatId = globalBanItem.BannedFrom;
        // var reason = globalBanItem.ReasonBan;
        //
        // var data = new Dictionary<string, object>()
        // {
        //     { "user_id", userId },
        //     { "from_id", fromId },
        //     { "chat_id", chatId },
        //     { "reason", reason }
        // };
        //
        // Log.Information("Inserting new GBan: {@V}", globalBanItem);
        //
        // var query = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(GBanTable)
        //     .InsertAsync(data);

        await DB.InsertAsync(new GlobalBanUserEntity()
        {
            BannedUserId = globalBanItem.UserId,
            UserId = globalBanItem.BannedBy,
            Reason = globalBanItem.ReasonBan,
            ChatId = globalBanItem.BannedFrom
        });

        return true;
    }

    /// <summary>
    /// Deletes the ban async.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>Delete GBan by userId</returns>
    public async Task<bool> DeleteBanAsync(long userId)
    {
        // var delete = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(GBanTable)
        //     .Where("user_id", userId)
        //     .DeleteAsync();

        var deleteResult = await DB.DeleteAsync<GlobalBanUserEntity>(entity =>
            entity.BannedUserId == userId
        );

        return deleteResult.DeletedCount > 0;
    }

    /// <summary>
    /// Gets the global ban from db by id.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>Banned user by userId</returns>
    public async Task<GlobalBanItem> GetGlobalBanByIdCore(long userId)
    {
        var where = new Dictionary<string, object>()
        {
            { "user_id", userId }
        };

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(GBanTable)
            .Where(where)
            .FirstOrDefaultAsync<GlobalBanItem>();

        return query;
    }

    /// <summary>
    /// Gets the global ban by id (cached).
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A Task.</returns>
    public async Task<GlobalBanItem> GetGlobalBanById(long userId)
    {
        var cacheKey = GetCacheKey(userId);

        var data = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => {
                var data = await GetGlobalBanByIdCore(userId);
                return data;
            }
        );

        return data;
    }

    /// <summary>
    /// Gets the global ban from db.
    /// </summary>
    /// <returns>A IEnumerable GlobalBanData</returns>
    public async Task<IEnumerable<GlobalBanItem>> GetGlobalBanCore()
    {
        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(GBanTable)
            .GetAsync<GlobalBanItem>();

        return query;
    }

    /// <summary>
    /// Gets the global bans.
    /// </summary>
    /// <returns>Get all Global Bans user</returns>
    public async Task<IEnumerable<GlobalBanItem>> GetGlobalBans()
    {
        var cacheKey = "global-bans";

        var datas = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => {
                var datas = await GetGlobalBanCore();
                return datas;
            }
        );

        return datas;
    }

    public async Task UpdateCache(long userId = -1)
    {
        if (userId == -1)
        {
            await _cacheService.EvictAsync(CacheKey);
            await GetGlobalBans();
        }
        else
        {
            var cacheKey = GetCacheKey(userId);
            await _cacheService.EvictAsync(cacheKey);

            await GetGlobalBanById(userId);
        }
    }

    public async Task<int> ImportFile(
        string fileName,
        GlobalBanItem globalBan
    )
    {
        var reader = fileName.ReadCsv<CommonGlobalBanItem>();

        var gbanMaps = reader.Select
        (
            item => new GlobalBanItem()
            {
                UserId = item.UserId,
                BannedBy = globalBan.BannedBy,
                BannedFrom = globalBan.BannedFrom,
                ReasonBan = globalBan.ReasonBan,
                CreatedAt = DateTime.Now
            }
        ).ToList();

        var deleteRows = await _queryService
            .CreateMySqlFactory()
            .FromTable(GBanTable)
            .WhereIn("user_id", gbanMaps.Select(x => x.UserId))
            .DeleteAsync();

        var insertRows = await _queryService
            .CreateMySqlFactory()
            .FromTable(GBanTable)
            .InsertAsync(
                columns: new[]
                {
                    "user_id", "from_id", "chat_id", "reason", "created_at"
                },
                valuesCollection: gbanMaps.Select
                (
                    item => new object[]
                    {
                        item.UserId, item.BannedBy, item.BannedFrom, item.ReasonBan, item.CreatedAt
                    }
                )
            );

        var diff = insertRows - deleteRows;

        return diff;
    }

    #region GBan Admin

    public async Task<bool> IsGBanAdminAsync(long userId)
    {
        var querySql = await _queryService
            .CreateMySqlFactory()
            .FromTable(GBanAdminTable)
            .Where("user_id", userId)
            .GetAsync<GlobalBanAdminItem>();

        var isRegistered = querySql.Any();
        Log.Debug(
            "UserId {UserId} is registered on ES2? {IsRegistered}",
            userId,
            isRegistered
        );

        return isRegistered;
    }

    public async Task RegisterAdminAsync(GlobalBanAdminItem globalBanAdminItem)
    {
        var querySql = _queryService
            .CreateMySqlFactory()
            .FromTable(GBanAdminTable)
            .Where("user_id", globalBanAdminItem.UserId);

        var get = await querySql.GetAsync();

        if (get.Any())
        {
            await querySql.InsertAsync
            (
                new Dictionary<string, object>()
                {
                    { "", "" }
                }
            );
        }
    }

    public async Task SaveAdminBan(GlobalBanAdminItem adminItem)
    {
        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(GBanAdminTable)
            .InsertAsync(adminItem);

        Log.Debug("Insert GBanReg: {Insert}", insert);
    }

    #endregion
}