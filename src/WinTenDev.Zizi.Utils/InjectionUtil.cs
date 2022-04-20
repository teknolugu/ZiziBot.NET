using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace WinTenDev.Zizi.Utils;

public static class InjectionUtil
{
    private static IServiceProvider _serviceProvider;

    // Reference: https://www.davidezoccarato.cloud/resolving-instances-with-asp-net-core-di-in-static-classes/
    public static void UseServiceInjection(this IApplicationBuilder serviceProvider)
    {
        _serviceProvider = serviceProvider.ApplicationServices;
    }

    public static IServiceScope GetScope()
    {
        return _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
    }

    public static IServiceProvider GetServiceProvider(this IApplicationBuilder app)
    {
        var serviceScope = app.ApplicationServices.CreateScope();
        return serviceScope.ServiceProvider;
    }

    public static TService GetRequiredService<TService>(this IApplicationBuilder app)
    {
        var appService = app.GetServiceProvider()
            .GetRequiredService<TService>();

        return appService;
    }
}
