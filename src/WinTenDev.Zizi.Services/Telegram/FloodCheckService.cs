using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NMemory.Linq;
using NMemory.Tables;
using SerilogTimings;

namespace WinTenDev.Zizi.Services.Telegram;

public class FloodCheckService
{
    private readonly ILogger<FloodCheckService> _logger;
    private readonly HitActivityInMemory _hitActivityInMemory;
    private readonly QueryService _queryService;
    private readonly ITable<HitActivity> _hitActivities;

    public FloodCheckService(
        ILogger<FloodCheckService> logger,
        HitActivityInMemory hitActivityInMemory,
        QueryService queryService
    )
    {
        _logger = logger;
        _hitActivityInMemory = hitActivityInMemory;
        _queryService = queryService;

        _hitActivities = hitActivityInMemory.FloodActivities;
    }

    public FloodCheckResult RunFloodCheck(HitActivity hitActivity)
    {
        var chatId = hitActivity.ChatId;
        var userId = hitActivity.FromId;

        SaveHitActivity(hitActivity);

        var floodCheck = IsFlood(chatId, userId);
        // if (!floodCheck.IsFlood)
        // {
        // }

        // if (floodCheck.IsFlood)
        // {
        // }
        RemoveOldActivities(chatId, userId);

        return floodCheck;
    }

    public FloodCheckResult IsFlood(
        long chatId,
        long userId
    )
    {
        const float floodOffset = 3;
        var lastActivities = GetLastActivity(chatId, userId);
        var itemCount = lastActivities.Count();
        var floodRate = itemCount / floodOffset;
        var isFlood = floodRate > 1;

        _logger.LogDebug(
            "UserId {UserId} is Flood at ChatId {ChatId}? {IsFlood}. Rate: {Rate}. Item: {Item}",
            userId,
            chatId,
            isFlood,
            floodRate,
            itemCount
        );

        var floodResult = new FloodCheckResult()
        {
            IsFlood = isFlood,
            FloodRate = floodRate,
            LastActivitiesCount = itemCount
        };

        return floodResult;
    }

    public void SaveHitActivity(HitActivity hitActivity)
    {
        var op = Operation.Begin(
            "Saving Flood activity for UserId: {UserId} at ChatId: {ChatId}",
            hitActivity.FromId,
            hitActivity.ChatId
        );
        _hitActivities.Insert(hitActivity);

        op.Complete();
    }

    public IEnumerable<HitActivity> GetAllHitActivities()
    {
        var allHitActivities = _hitActivities.AsEnumerable();

        return allHitActivities;
    }

    public IEnumerable<HitActivity> GetLastActivity(
        long chatId,
        long userId
    )
    {
        var getLastActivities = _hitActivities
            .AsEnumerable()
            .Where
            (
                x =>
                    x.ChatId == chatId &&
                    x.FromId == userId &&
                    x.Timestamp >= DateTime.Now.AddSeconds(-12)
            );

        _logger.LogDebug(
            "Get Last Activities for {UserId} at ChatId {ChatId}",
            userId,
            chatId
        );

        return getLastActivities;
    }

    public void RemoveOldActivities(
        long chatId,
        long userId
    )
    {
        // var beforeDel = GetAllHitActivities();

        var remove = _hitActivities
            .Where
            (
                x =>
                    (x.FromId == userId &&
                     x.ChatId == chatId &&
                     x.Timestamp <= DateTime.Now.AddSeconds(-10)) ||
                    x.Timestamp <= DateTime.Now.AddMinutes(-60)
            )
            .Delete();

        // var afterDel = GetAllHitActivities();

        _logger.LogDebug(
            "Remove for {UserId} at ChatId {ChatId}. Result: {Result}. Prev: {BeforeDel}: After: {AfterDel}",
            userId,
            chatId,
            remove,
            _hitActivities.Count,
            _hitActivities.Count
        );
    }
}