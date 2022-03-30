using System;
using CacheTower;
using CacheTower.Extensions;
using CacheTower.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class CacheTowerServiceExtension
{
    public static IServiceCollection AddCacheTower(this IServiceCollection services)
    {
        var cacheTowerPath = "Storage/Cache-Tower/".EnsureDirectory();
        var serviceProvider = services.BuildServiceProvider();
        var cacheConfig = serviceProvider.GetRequiredService<IOptions<CacheConfig>>().Value;

        var cacheLayers = new ICacheLayer[]
        {
            new MemoryCacheLayer()
        };

        if (cacheConfig.InvalidateOnStart)
        {
            cacheTowerPath.DeleteDirectory().EnsureDirectory();
        }

        services.AddSingleton(
            _ => {
                var stack = new CacheStack(
                    cacheLayers: cacheLayers,
                    extensions: new ICacheExtension[]
                    {
                        new AutoCleanupExtension(TimeSpan.FromMinutes(cacheConfig.ExpireAfter))
                    }
                );

                return stack;
            }
        );

        return services;
    }
}
