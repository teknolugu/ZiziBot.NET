using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx.Synchronous;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Extensions;

public static class HangfireJobsExtension
{
    public static IApplicationBuilder RegisterHangfireJobs(this IApplicationBuilder app)
    {
        HangfireUtil.DeleteAllJobs();

        var serviceProvider = app.GetServiceProvider();
        var jobService = serviceProvider.GetRequiredService<JobsService>();

        serviceProvider.GetRequiredService<StorageService>().ResetHangfireRedisStorage().WaitAndUnwrapException();
        serviceProvider.GetRequiredService<RssFeedService>().RegisterJobAllRssScheduler().InBackground();
        serviceProvider.GetRequiredService<EpicGamesService>().RegisterJobEpicGamesBroadcaster().InBackground();
        serviceProvider.GetRequiredService<ShalatTimeNotifyService>().RegisterJobShalatTimeAsync().InBackground();

        jobService.ClearPendingJobs();
        jobService.RegisterJobChatCleanUp().InBackground();
        jobService.RegisterJobClearLog();
        jobService.RegisterJobDeleteOldStep();
        jobService.RegisterJobDeleteOldRssHistory();
        jobService.RegisterJobDeleteOldMessageHistory();

        var botService = app.GetRequiredService<BotService>();
        var botEnvironment = botService.CurrentEnvironment()
            .Result;

        // This job enabled for non Production,
        // Daily demote for free Administrator at Testing Group
        if (botEnvironment != BotEnvironmentLevel.Production)
        {
            serviceProvider.GetRequiredService<JobsService>()
                .RegisterJobAdminCleanUp();
        }

        return app;
    }
}