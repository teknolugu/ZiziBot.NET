using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Extensions;

public static class HangfireJobsExtension
{
    public static IApplicationBuilder RegisterHangfireJobs(this IApplicationBuilder app)
    {
        HangfireUtil.DeleteAllJobs();

        var appService = app.GetServiceProvider();

        appService.GetRequiredService<RssFeedService>().RegisterJobAllRssScheduler().InBackground();

        appService.GetRequiredService<JobsService>().RegisterJobChatCleanUp().InBackground();
        appService.GetRequiredService<JobsService>().RegisterJobClearLog();
        appService.GetRequiredService<JobsService>().RegisterJobDeleteOldStep();

        return app;
    }
}