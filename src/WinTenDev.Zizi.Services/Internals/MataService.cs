using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using SerilogTimings;

namespace WinTenDev.Zizi.Services.Internals;

public class MataService
{
    private readonly ILogger<MataService> _logger;
    private readonly QueryService _queryService;

    public MataService(
        ILogger<MataService> logger,
        QueryService queryService
    )
    {
        _logger = logger;
        _queryService = queryService;
    }

    public async Task<UserInfoEntity> GetLastMataAsync(long userId)
    {
        var op = Operation.Begin("Getting last User History for {UserId}", userId);

        var lastActivity = await DB.Find<UserInfoEntity>()
            .Match(info => info.UserId == userId)
            .Sort(info => info.CreatedOn, Order.Descending)
            .ExecuteFirstAsync();

        if (lastActivity == null)
        {
            op.Complete();
            return null;
        }

        op.Complete();
        return lastActivity;
    }

    public async Task<UserInfoEntity> GetLastMataAsync(Expression<Func<UserInfoEntity, bool>> expression)
    {
        var op = Operation.Begin("Getting last User History with expression {UserId}", expression);

        var lastActivity = await DB.Find<UserInfoEntity>()
            .Match(expression)
            .Sort(info => info.CreatedOn, Order.Descending)
            .ExecuteFirstAsync();

        if (lastActivity == null)
        {
            op.Complete();
            return null;
        }

        op.Complete();
        return lastActivity;
    }

    public async Task SaveMataAsync(UserInfoEntity userInfo)
    {
        var op = Operation.Begin("Saving User History for {UserId}", userInfo.UserId);

        await DB.InsertAsync(userInfo);

        op.Complete();
    }

    public async Task DeleteAsync()
    {
        var deleteResult = await DB.DeleteAsync<UserInfoEntity>(builder =>
            builder.CreatedOn < DateTime.Now.AddMonths(-6)
        );

        _logger.LogDebug("Deleted result: {@Count} UserInfo records", deleteResult);
    }
}