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

        var serviceProvider = app.GetServiceProvider();

        serviceProvider.GetRequiredService<RssFeedService>().RegisterJobAllRssScheduler().InBackground();

        serviceProvider.GetRequiredService<JobsService>().RegisterJobChatCleanUp().InBackground();
        serviceProvider.GetRequiredService<JobsService>().RegisterJobClearLog();
        serviceProvider.GetRequiredService<JobsService>().RegisterJobDeleteOldStep();
        serviceProvider.GetRequiredService<JobsService>().RegisterJobDeleteOldRssHistory();
        serviceProvider.GetRequiredService<JobsService>().RegisterJobDeleteOldMessageHistory();

        return app;
    }
}