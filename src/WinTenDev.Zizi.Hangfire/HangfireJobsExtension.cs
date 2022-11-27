using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using SerilogTimings;

namespace WinTenDev.Zizi.Hangfire;

public static class HangfireJobsExtension
{
    public static async Task<IApplicationBuilder> RegisterHangfireJobs(this IApplicationBuilder app)
    {
        var op = Operation.At(LogEventLevel.Warning).Begin("Registering Hangfire Jobs");

        var serviceProvider = app.GetServiceProvider();
        var jobService = serviceProvider.GetRequiredService<JobsService>();

        HangfireUtil.PurgeJobs();
        jobService.ClearPendingJobs();
        await jobService.RegisterJobChatCleanUp();

        await serviceProvider.GetRequiredService<StorageService>().ResetHangfireRedisStorage();
        await serviceProvider.GetRequiredService<RssFeedService>().RegisterJobAllRssScheduler();
        await serviceProvider.GetRequiredService<EpicGamesService>().RegisterJobEpicGamesBroadcaster();
        await serviceProvider.GetRequiredService<ShalatTimeNotifyService>().RegisterJobShalatTimeAsync();

        jobService.RegisterJobClearLog();
        jobService.RegisterJobClearTempFiles();
        jobService.RegisterJobDeleteOldStep();
        jobService.RegisterJobDeleteOldRssHistory();
        jobService.RegisterJobDeleteOldMessageHistory();
        jobService.RegisterJobRunMongoDbBackup();
        jobService.RegisterJobRunMysqlBackup();
        jobService.RegisterJobRunDeleteOldUpdates();

        var botService = app.GetRequiredService<BotService>();
        var botEnvironment = await botService.CurrentEnvironment();

        // This job enabled for non Production,
        // Daily demote for free Administrator at Testing Group
        if (botEnvironment != BotEnvironmentLevel.Production)
        {
            jobService.RegisterJobAdminCleanUp();
        }

        op.Complete();

        return app;
    }
}