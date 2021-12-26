using DotNurse.Injector;
using Microsoft.Extensions.DependencyInjection;

namespace WinTenDev.ZiziBot.AppHost.Extensions;

public static class CommandHandlerExtension
{
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        const string path = "WinTenDev.ZiziBot.AppHost.Handlers";

        services.AddServicesFrom(type =>
            (type.Namespace?.StartsWith(path) ?? false) &&
            (!type.FullName?.EndsWith("Base") ?? false),
        ServiceLifetime.Scoped);

        return services;
    }
}