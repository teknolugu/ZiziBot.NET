using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Services.Internals;

public class UserExceptionService
{
    private readonly ILogger<UserExceptionService> _logger;

    public UserExceptionService(ILogger<UserExceptionService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsExist(
        long chatId,
        long userId
    )
    {
        var isExist = await DB.Find<UserExceptionEntity>()
            .Match(entity =>
                entity.ChatId == chatId &&
                entity.UserId == userId
            )
            .ExecuteAnyAsync();

        _logger.LogInformation("UserId: {UserId} is GlobalException? {IsGlobalException}", userId, isExist);

        return isExist;
    }

    public async Task<int> Save(UserExceptionEntity data)
    {
        var result = await await DB.Find<UserExceptionEntity>()
            .Match(entity =>
                entity.ChatId == data.ChatId &&
                entity.UserId == data.UserId
            )
            .ExecuteAnyAsync()
            .ContinueWith(async task => {
                if (task.Result) return 0;

                await data.InsertAsync();

                return 1;
            })
            .ConfigureAwait(false);

        return result;
    }
}