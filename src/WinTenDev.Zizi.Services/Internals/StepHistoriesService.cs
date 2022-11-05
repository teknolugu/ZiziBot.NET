using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Humanizer;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using RepoDb;

namespace WinTenDev.Zizi.Services.Internals;

public class StepHistoriesService
{
    private readonly ILogger<StepHistoriesService> _logger;
    private readonly IMapper _mapper;
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    public StepHistoriesService(
        ILogger<StepHistoriesService> logger,
        IMapper mapper,
        CacheService cacheService,
        QueryService queryService
    )
    {
        _logger = logger;
        _mapper = mapper;
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
        var data = await _cacheService.GetOrSetAsync(
            cacheKey: CacheKey(chatId),
            action: () =>
                GetStepHistoryCore(
                    chatId,
                    userId,
                    name
                )
        );

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
            .QueryAsync<StepHistory>(
                history =>
                    history.ChatId == chatId &&
                    history.UserId == userId &&
                    history.Name == name
            );

        return data.FirstOrDefault();
    }

    public async Task<StepHistory> GetStepHistoryCore(StepHistory stepHistory)
    {
        var data = await _queryService
            .CreateMysqlConnectionCore()
            .QueryAsync<StepHistory>(
                history =>
                    history.ChatId == stepHistory.ChatId &&
                    history.UserId == stepHistory.UserId
            );

        return data.FirstOrDefault();
    }

    public async Task<IEnumerable<StepHistory>> GetStepHistoryAllCore(StepHistory stepHistory)
    {
        var data = await _queryService
            .CreateMysqlConnectionCore()
            .QueryAsync<StepHistory>(
                history =>
                    history.ChatId == stepHistory.ChatId &&
                    history.UserId == stepHistory.UserId
            );

        return data;
    }

    public async Task<List<StepHistoryEntity>> GetStepHistoryVerifyCore(StepHistoryDto stepHistoryDto)
    {
        // var data = await _queryService
        //     .CreateMysqlConnectionCore()
        //     .QueryAsync<StepHistory>(
        //         history =>
        //             history.ChatId == stepHistoryDto.ChatId &&
        //             history.UserId == stepHistoryDto.UserId &&
        //             history.Status == StepHistoryStatus.NeedVerify
        //     );

        var data = await DB.Find<StepHistoryEntity>()
            .Match(entity =>
                entity.ChatId == stepHistoryDto.ChatId &&
                entity.UserId == stepHistoryDto.UserId &&
                entity.Status == StepHistoryStatus.NeedVerify)
            .ExecuteAsync();

        return data;
    }

    public async Task<object> SaveStepHistory(StepHistory stepHistory)
    {
        var chatId = stepHistory.ChatId;
        var userId = stepHistory.UserId;
        _logger.LogInformation(
            "Saving StepHistory for UserId: {UserId} at {ChatId}",
            userId,
            chatId
        );

        var isExist = await _queryService
            .CreateMysqlConnectionCore()
            .ExistsAsync<StepHistory>(
                history =>
                    history.ChatId == stepHistory.ChatId &&
                    history.UserId == stepHistory.UserId &&
                    history.Name == stepHistory.Name
            );

        if (isExist)
        {
            var update = await _queryService
                .CreateMysqlConnectionCore()
                .UpdateAsync(
                    stepHistory,
                    history =>
                        history.ChatId == stepHistory.ChatId &&
                        history.UserId == stepHistory.UserId &&
                        history.Name == stepHistory.Name
                );

            _logger.LogDebug(
                "Update StepHistory for UserId: {UserId} at {ChatId}. {Update}",
                userId,
                chatId,
                update
            );
        }
        else
        {
            var insert = await _queryService
                .CreateMysqlConnectionCore()
                .InsertAsync(stepHistory);

            _logger.LogDebug(
                "Insert StepHistory for UserId: {UserId} at {ChatId}. {Insert}",
                userId,
                chatId,
                insert
            );
        }

        UpdateCache(chatId, userId).InBackground();

        return true;
    }

    public async Task<object> SaveStepHistory(StepHistoryDto stepHistoryDto)
    {
        var stepHistoryEntity = _mapper.Map<StepHistoryEntity>(stepHistoryDto);

        await stepHistoryEntity.ExSaveAsync(entity =>
            entity.ChatId == stepHistoryDto.ChatId
            && entity.UserId == stepHistoryDto.UserId
            && entity.Name == stepHistoryDto.Name
        );

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
            .DeleteAsync<StepHistory>(
                history =>
                    history.ChatId == chatId &&
                    history.UserId == userId &&
                    history.Name == name
            );

        _logger.LogDebug(
            "Delete StepHistory for UserId: {UserId} at {ChatId}. {Delete}",
            userId,
            chatId,
            delete
        );

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
            .UpdateAsync(
                entity,
                fields: fields,
                where: history =>
                    history.ChatId == entity.ChatId &&
                    history.UserId == entity.UserId
            );

        UpdateCache(entity.ChatId, entity.UserId).InBackground();

        return update;
    }

    public async Task<int> UpdateStepHistoryStatus(StepHistory entity)
    {
        var fields = Field.Parse<StepHistory>(
            history => new
            {
                history.Status
            }
        );

        var update = await _queryService
            .CreateMysqlConnectionCore()
            .UpdateAsync(
                entity,
                fields: fields,
                where: history =>
                    history.ChatId == entity.ChatId &&
                    history.UserId == entity.UserId
            );

        return update;
    }

    [JobDisplayName("Delete olds Step History")]
    public async Task DeleteOldStepHistory()
    {
        var delete = await _queryService
            .CreateMysqlConnectionCore()
            .DeleteAsync<StepHistory>(
                history =>
                    history.Status != StepHistoryStatus.NeedVerify ||
                    history.UpdatedAt >= DateTime.Now.AddDays(-2)
            );

        _logger.LogInformation("Removed old Step data. {Total}", "Item".ToQuantity(delete));
    }

    private async Task<IEnumerable<StepHistory>> UpdateCache(
        long chatId,
        long userId
    )
    {
        var data = await _cacheService.SetAsync(
            cacheKey: CacheKey(chatId),
            action: async () => {
                var data = await _queryService
                    .CreateMysqlConnectionCore()
                    .QueryAsync<StepHistory>(
                        history =>
                            history.ChatId == chatId &&
                            history.UserId == userId
                    );

                return data;
            }
        );

        return data;
    }
}