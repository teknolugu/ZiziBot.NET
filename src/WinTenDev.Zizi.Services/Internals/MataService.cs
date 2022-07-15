using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

    public async Task<UserHistory> GetLastMataAsync(long userId)
    {
        var op = Operation.Begin("Getting last User History for {UserId}", userId);

        var instance = await _queryService.GetMongoRealmInstance();
        var userHistory = instance.All<UserHistory>().Where(history => history.FromId == userId);
        var lastActivity = userHistory.LastOrDefault();

        if (lastActivity == null)
        {
            op.Complete();
            return null;
        }

        await instance.WriteAsync(
            realm => {
                var forDelete = userHistory
                    .Where(history => history.Timestamp < lastActivity.Timestamp);
                realm.RemoveRange(forDelete);
            }
        );

        op.Complete();
        return lastActivity;
    }

    public async Task SaveMataAsync(UserHistory userHistory)
    {
        var op = Operation.Begin("Saving User History for {UserId}", userHistory.FromId);

        var instance = await _queryService.GetMongoRealmInstance();
        await instance.WriteAsync(
            realm => {
                realm.Add(userHistory);
            }
        );

        op.Complete();
    }
}