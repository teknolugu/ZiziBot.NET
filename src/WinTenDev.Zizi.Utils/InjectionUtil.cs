using System;
using System.Collections.Generic;
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

    public static TService GetRequiredService<TService>(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var appService = serviceProvider.GetRequiredService<TService>();

        return appService;
    }

    public static TService GetRequiredService<TService>()
    {
        var serviceScope = InjectionUtil.GetScope();
        var serviceProvider = serviceScope.ServiceProvider;
        var resolvedService = serviceProvider.GetRequiredService<TService>();

        return resolvedService;
    }

    public static IEnumerable<TService> GetServices<TService>(this IApplicationBuilder app)
    {
        var services = app.ApplicationServices.GetServices<TService>();

        return services;
    }
}