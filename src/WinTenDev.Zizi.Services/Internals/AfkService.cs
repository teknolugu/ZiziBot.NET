using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class AfkService
{
    private const string BaseTable = "afk";
    private const string CacheKey = "afk";
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    public AfkService(
        CacheService cacheService,
        QueryService queryService
    )
    {
        _cacheService = cacheService;
        _queryService = queryService;
    }

    /// <summary>Determines whether AFK is exist by User ID</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>
    ///     true if AFK exist otherwise, false.
    /// </returns>
    public async Task<bool> IsExistCore(long userId)
    {
        var user = await GetAfkByIdCore(userId);
        var isExist = user != null;
        Log.Debug(
            "Is UserId: '{UserId}' AFK? {IsExist}",
            userId,
            isExist
        );

        return isExist;
    }

    /// <summary>Gets the afk all core.</summary>
    /// <returns>
    ///   Return All AFK bot-wide (un-cached)
    /// </returns>
    public async Task<IEnumerable<Afk>> GetAfkAllCore()
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            .GetAsync<Afk>();

        return data;
    }

    /// <summary>Gets the afk by identifier core.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>
    ///   Return single AFK row by User ID (un-cached)
    /// </returns>
    public async Task<Afk> GetAfkByIdCore(long userId)
    {
        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            .Where("user_id", userId)
            .FirstOrDefaultAsync<Afk>();

        return data;
    }

    /// <summary>Gets the afk by identifier.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>Return AFK by User ID (cached)</returns>
    public async Task<Afk> GetAfkById(long userId)
    {
        var key = CacheKey + $"-{userId}";

        var data = await _cacheService.GetOrSetAsync(
            cacheKey: key,
            action: async () => {
                var afk = await GetAfkByIdCore(userId);
                return afk;
            }
        );

        return data;
    }

    /// <summary>Updates the AFK Cache by User ID</summary>
    /// <param name="userId">The user identifier.</param>
    public async Task UpdateAfkByIdCacheAsync(long userId)
    {
        var key = CacheKey + $"-{userId}";
        var afk = await GetAfkByIdCore(userId);

        await _cacheService.SetAsync(key, afk);
    }

    /// <summary>Save AFK data to Database</summary>
    /// <param name="data">The dictionary of AFK data.</param>
    public async Task SaveAsync(Dictionary<string, object> data)
    {
        Log.Information("Save: {V}", data.ToJson());

        var where = new Dictionary<string, object>()
        {
            { "user_id", data["user_id"] }
        };

        int saveResult;

        var isExist = await IsExistCore(@where["user_id"].ToInt64());

        if (isExist)
        {
            saveResult = await _queryService
                .CreateMySqlFactory()
                .FromTable(BaseTable)
                .Where(where)
                .UpdateAsync(data);
        }
        else
        {
            saveResult = await _queryService
                .CreateMySqlFactory()
                .FromTable(BaseTable)
                .InsertAsync(data);
        }

        Log.Information("SaveAfk: {Insert}", saveResult);
    }
}