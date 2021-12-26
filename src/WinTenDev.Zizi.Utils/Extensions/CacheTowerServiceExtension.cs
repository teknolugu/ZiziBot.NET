using System;
using CacheTower;
using CacheTower.Extensions;
using CacheTower.Providers.FileSystem.Json;
using CacheTower.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class CacheTowerServiceExtension
{
    public static IServiceCollection AddCacheTower(this IServiceCollection services)
    {
        var cacheTowerPath = "Storage/Cache-Tower/".EnsureDirectory();

        services.AddSingleton(_ => {
            var stack = new CacheStack(new ICacheLayer[]
            {
                new MemoryCacheLayer(),
                new JsonFileCacheLayer(cacheTowerPath)
            }, new ICacheExtension[]
            {
                new AutoCleanupExtension(TimeSpan.FromMinutes(2))
            });

            return stack;
        });

        return services;
    }
}