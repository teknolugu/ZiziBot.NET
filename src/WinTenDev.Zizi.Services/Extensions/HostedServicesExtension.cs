using Microsoft.Extensions.DependencyInjection;

namespace WinTenDev.Zizi.Services.Extensions;

public static class HostedServicesExtension
{
    public static IServiceCollection AddHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<HttpTunnelingHostedService>();

        return services;
    }
}
