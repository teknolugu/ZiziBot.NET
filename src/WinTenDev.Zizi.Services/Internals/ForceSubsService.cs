using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Services.Internals;

public class ForceSubsService
{
    private readonly ILogger<ForceSubsService> _logger;

    public ForceSubsService(ILogger<ForceSubsService> logger)
    {
        _logger = logger;
    }

    public async Task<int> SaveSubsAsync(ForceSubscriptionEntity forceSubscription)
    {
        int affectedRows = 0;
        var chatId = forceSubscription.ChatId;
        var channelId = forceSubscription.ChannelId;

        var currentSubscriptions = await DB.Find<ForceSubscriptionEntity>()
            .ManyAsync(
                subscription =>
                    subscription.ChatId == chatId &&
                    subscription.ChannelId == channelId
            );

        if (currentSubscriptions.Count == 0)
        {
            await DB.SaveAsync<ForceSubscriptionEntity>(forceSubscription);
            affectedRows = 1;
        }

        return affectedRows;
    }

    public async Task<List<ForceSubscriptionEntity>> GetSubsAsync(long chatId)
    {
        var subscriptions = await DB.Find<ForceSubscriptionEntity>()
            .ManyAsync(
                subscription =>
                    subscription.ChatId == chatId
            );

        _logger.LogDebug(
            "Found {Count} subscriptions for ChatId {ChatId}",
            subscriptions.Count,
            chatId
        );

        return subscriptions;
    }

    public async Task<DeleteResult> DeleteSubsAsync(
        long chatId,
        long channelId
    )
    {
        var deleteResult = await DB.DeleteAsync<ForceSubscriptionEntity>(
            subscription =>
                subscription.ChatId == chatId &&
                subscription.ChannelId == channelId
        );

        _logger.LogDebug("Deleted {0} subscriptions", deleteResult.DeletedCount);

        return deleteResult;
    }
}