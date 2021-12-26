using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RepoDb;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals;

public class StepHistoriesService
{
    private readonly ILogger<StepHistoriesService> _logger;
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    public StepHistoriesService(
        ILogger<StepHistoriesService> logger,
        CacheService cacheService,
        QueryService queryService
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _queryService = queryService;
    }

    private string CacheKey(long chatId)
    {
        var key = $"step-histories_{chatId}";

        return key;
    }

    public async Task<IEnumerable<StepHistory>> GetStepHistory(long chatId)
    {
        var connection = _queryService.CreateMysqlConnectionCore();

        var data = await _cacheService.GetOrSetAsync(CacheKey(chatId), async () => {
            var data = await connection.QueryAsync<StepHistory>(x =>
                x.ChatId == chatId
            );

            return data;
        });

        return data;
    }

    public async Task<object> SaveStep(StepHistory stepHistory)
    {
        var chatId = stepHistory.ChatId;
        var userId = stepHistory.UserId;
        _logger.LogInformation("Starting to save step history for ChatId: {ChatId}, UserId: {UserId}", chatId, userId);

        var connection = _queryService.CreateMysqlConnectionCore();

        var isExist = await connection.ExistsAsync<StepHistory>(x =>
            x.ChatId == stepHistory.ChatId
            && x.UserId == stepHistory.UserId
        );

        if (isExist)
        {
            _logger.LogDebug("Updating step history for ChatId: {ChatId}, UserId: {UserId}", chatId, userId);
            var update = await connection.UpdateAsync(stepHistory, x =>
                x.ChatId == stepHistory.ChatId && x.UserId == stepHistory.UserId
            );

            _logger.LogDebug("Update step history for ChatId: {ChatId}, UserId: {UserId}  result: {Update}", chatId, userId, update);
        }
        else
        {
            _logger.LogDebug("Inserting step history for");
            var insert = await connection.InsertAsync<StepHistory>(stepHistory);

            _logger.LogDebug("Insert step history for ChatId: {ChatId}, UserId: {UserId}  result: {Insert}", chatId, userId, insert);
        }

        UpdateCache(chatId).InBackground();

        return true;
    }

    public async Task UpdateCache(long chatId)
    {
        // _logger.LogDebug("Evicting cache step history for ChatId: {ChatId}", chatId);
        // await _cacheService.EvictAsync(CacheKey(chatId));

        var connection = _queryService.CreateMysqlConnectionCore();

        var data = await _cacheService.SetAsync(CacheKey(chatId), async () => {
            var data = await connection.QueryAsync<StepHistory>(x =>
                x.ChatId == chatId
            );

            return data;
        });
    }
}