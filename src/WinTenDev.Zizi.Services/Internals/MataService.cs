using System;
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

    public async Task<UserInfo> GetLastMataAsync(long userId)
    {
        var op = Operation.Begin("Getting last User History for {UserId}", userId);

        var lastActivity = await DB.Find<UserInfo>()
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

    public async Task SaveMataAsync(UserInfo userInfo)
    {
        var op = Operation.Begin("Saving User History for {UserId}", userInfo.UserId);

        await DB.InsertAsync(userInfo);

        op.Complete();
    }

    public async Task DeleteAsync()
    {
        var deleteResult = await DB.DeleteAsync<UserInfo>(builder =>
            builder.CreatedOn < DateTime.Now.AddMonths(-6)
        );

        _logger.LogDebug("Deleted result: {@Count} UserInfo records", deleteResult);
    }
}