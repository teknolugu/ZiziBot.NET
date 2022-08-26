using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MongoDB.Entities;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class AfkService
{
    private const string BaseTable = "afk";
    private const string CacheKey = "afk";
    private readonly IMapper _mapper;
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    public AfkService(
        IMapper mapper,
        CacheService cacheService,
        QueryService queryService
    )
    {
        _mapper = mapper;
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
    public async Task<AfkEntity> GetAfkByIdCore(long userId)
    {
        // var data = await _queryService
        //     .CreateMySqlFactory()
        //     .FromTable(BaseTable)
        //     .Where("user_id", userId)
        //     .FirstOrDefaultAsync<Afk>();

        var data = await DB.Find<AfkEntity>()
            .Match(entity => entity.UserId == userId)
            .ExecuteFirstAsync();

        return data;
    }

    /// <summary>Gets the afk by identifier.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="evictBefore">If true, Cache will be invalidated before Get</param>
    /// <returns>Return AFK by User ID (cached)</returns>
    public async Task<AfkEntity> GetAfkById(
        long userId,
        bool evictBefore = false
    )
    {
        var key = CacheKey + $"-{userId}";

        var data = await _cacheService.GetOrSetAsync(
            cacheKey: key,
            evictBefore: evictBefore,
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

    public async Task SaveAsync(AfkDto afkDto)
    {
        var data = _mapper.Map<AfkEntity>(afkDto);

        await DB.Find<AfkEntity>()
            .Match(entity => entity.UserId == afkDto.UserId)
            .ExecuteAnyAsync()
            .ContinueWith(async task => {
                if (task.Result)
                {
                    await DB.Update<AfkEntity>()
                        .Match(entity => entity.UserId == afkDto.UserId)
                        .ModifyExcept(entity => new { entity.ID, entity.CreatedOn }, data)
                        .ExecuteAsync();
                }
                else
                {
                    await data.InsertAsync();
                }
            });

        await GetAfkById(afkDto.UserId, evictBefore: true);
    }
}