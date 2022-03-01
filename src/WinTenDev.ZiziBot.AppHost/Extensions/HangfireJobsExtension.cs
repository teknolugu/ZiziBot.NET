using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Extensions;

public static class HangfireJobsExtension
{
    public static IApplicationBuilder RegisterHangfireJobs(this IApplicationBuilder app)
    {
        HangfireUtil.DeleteAllJobs();

        var serviceProvider = app.GetServiceProvider();
        var jobService = serviceProvider.GetRequiredService<JobsService>();

        serviceProvider.GetRequiredService<RssFeedService>().RegisterJobAllRssScheduler().InBackground();

        jobService.RegisterJobChatCleanUp().InBackground();
        jobService.RegisterJobClearLog();
        jobService.RegisterJobDeleteOldStep();
        jobService.RegisterJobDeleteOldRssHistory();
        jobService.RegisterJobDeleteOldMessageHistory();

        var botService = app.GetRequiredService<BotService>();
        var botEnvironment = botService.CurrentEnvironment()
            .Result;

        if (botEnvironment != BotEnvironmentLevel.Production)
        {
            serviceProvider.GetRequiredService<JobsService>()
                .RegisterJobAdminCleanUp();
        }

        return app;
    }
}