using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RepoDb;
using WinTenDev.Zizi.Models.Enums;
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

    public async Task<StepHistory> GetStepHistory(
        long chatId,
        long userId,
        StepHistoryName name
    )
    {
        var data = await _cacheService.GetOrSetAsync(CacheKey(chatId), () =>
            GetStepHistoryCore(chatId, userId, name));

        return data;
    }

    public async Task<StepHistory> GetStepHistoryCore(
        long chatId,
        long userId,
        StepHistoryName name
    )
    {
        var data = await _queryService
            .CreateMysqlConnectionCore()
            .QueryAsync<StepHistory>(history =>
                history.ChatId == chatId &&
                history.UserId == userId &&
                history.Name == name);

        return data.FirstOrDefault();
    }

    public async Task<StepHistory> GetStepHistoryCore(StepHistory stepHistory)
    {
        var data = await _queryService
            .CreateMysqlConnectionCore()
            .QueryAsync<StepHistory>(history =>
                history.ChatId == stepHistory.ChatId &&
                history.UserId == stepHistory.UserId);

        return data.FirstOrDefault();
    }

    public async Task<IEnumerable<StepHistory>> GetStepHistoryAllCore(StepHistory stepHistory)
    {
        var data = await _queryService
            .CreateMysqlConnectionCore()
            .QueryAsync<StepHistory>(history =>
                history.ChatId == stepHistory.ChatId &&
                history.UserId == stepHistory.UserId);

        return data;
    }

    public async Task<IEnumerable<StepHistory>> GetStepHistoryVerifyCore(StepHistory stepHistory)
    {
        var data = await _queryService
            .CreateMysqlConnectionCore()
            .QueryAsync<StepHistory>(history =>
                history.ChatId == stepHistory.ChatId &&
                history.UserId == stepHistory.UserId &&
                history.Status == StepHistoryStatus.NeedVerify);

        return data;
    }

    public async Task<object> SaveStepHistory(StepHistory stepHistory)
    {
        var chatId = stepHistory.ChatId;
        var userId = stepHistory.UserId;
        _logger.LogInformation("Saving StepHistory for UserId: {UserId} at {ChatId}", userId, chatId);

        var isExist = await _queryService
            .CreateMysqlConnectionCore()
            .ExistsAsync<StepHistory>(history =>
                history.ChatId == stepHistory.ChatId &&
                history.UserId == stepHistory.UserId &&
                history.Name == stepHistory.Name);

        if (isExist)
        {
            var update = await _queryService
                .CreateMysqlConnectionCore()
                .UpdateAsync(stepHistory, history =>
                    history.ChatId == stepHistory.ChatId &&
                    history.UserId == stepHistory.UserId &&
                    history.Name == stepHistory.Name);

            _logger.LogDebug("Update StepHistory for UserId: {UserId} at {ChatId}. {Update}", userId, chatId, update);
        }
        else
        {
            var insert = await _queryService
                .CreateMysqlConnectionCore()
                .InsertAsync(stepHistory);

            _logger.LogDebug("Insert StepHistory for UserId: {UserId} at {ChatId}. {Insert}", userId, chatId, insert);
        }

        UpdateCache(chatId, userId).InBackground();

        return true;
    }

    public async Task<int> DeleteStepHistory(
        long chatId,
        long userId,
        StepHistoryName name
    )
    {
        var delete = await _queryService
            .CreateMysqlConnectionCore()
            .DeleteAsync<StepHistory>(history =>
                history.ChatId == chatId &&
                history.UserId == userId &&
                history.Name == name);

        _logger.LogDebug("Delete StepHistory for UserId: {UserId} at {ChatId}. {Delete}", userId, chatId, delete);

        return delete;
    }

    public async Task<int> UpdateStepHistoryStatus(
        StepHistory entity,
        StepHistory where
    )
    {
        var update = await _queryService
            .CreateMysqlConnectionCore()
            .UpdateAsync(entity, where);

        UpdateCache(where.ChatId, where.UserId).InBackground();

        return update;
    }

    public async Task<int> UpdateStepHistoryStatus(
        StepHistory entity,
        IEnumerable<Field> fields
    )
    {
        var update = await _queryService
            .CreateMysqlConnectionCore()
            .UpdateAsync(entity, fields: fields, where: history =>
                history.ChatId == entity.ChatId &&
                history.UserId == entity.UserId);

        UpdateCache(entity.ChatId, entity.UserId).InBackground();

        return update;
    }

    public async Task<int> UpdateStepHistoryStatus(StepHistory entity)
    {
        var fields = Field.Parse<StepHistory>(history => new
        {
            history.Status
        });

        var update = await _queryService
            .CreateMysqlConnectionCore()
            .UpdateAsync(entity, fields: fields, where: history =>
                history.ChatId == entity.ChatId &&
                history.UserId == entity.UserId);

        return update;
    }

    private async Task<IEnumerable<StepHistory>> UpdateCache(
        long chatId,
        long userId
    )
    {
        var data = await _cacheService.SetAsync(CacheKey(chatId), async () => {
            var data = await _queryService
                .CreateMysqlConnectionCore()
                .QueryAsync<StepHistory>(history =>
                    history.ChatId == chatId &&
                    history.UserId == userId);

            return data;
        });

        return data;
    }
}