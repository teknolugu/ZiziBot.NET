using DalSoft.Hosting.BackgroundQueue.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace WinTenDev.Zizi.Services.Extensions;

public static class HostedServicesExtension
{
    public static IServiceCollection AddHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<HttpTunnelingHostedService>();

        services.AddBackgroundQueue(
            onException: exception => Log.Error(exception, "DalSoft.Hosting.BackgroundQueue"),
            maxConcurrentCount: 3,
            millisecondsToWaitBeforePickingUpTask: 1000
        );

        return services;
    }
}