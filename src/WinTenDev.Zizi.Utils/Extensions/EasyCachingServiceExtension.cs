using EasyCaching.Disk;
using EasyCaching.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Utils.Extensions;

/// <summary>
/// This Extension contains about configure of EasyCaching
/// </summary>
public static class EasyCachingServiceExtension
{
    /// <summary>
    /// Configure EasyCaching with SQLite
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEasyCachingSqlite(this IServiceCollection services)
    {
        services.AddEasyCaching
        (
            options => {
                options.UseSQLite
                (
                    sqLiteOptions => {
                        sqLiteOptions.EnableLogging = true;

                        sqLiteOptions.DBConfig = new SQLiteDBOptions()
                        {
                            CacheMode = SqliteCacheMode.Shared,
                            FilePath = "Storage/EasyCaching/".EnsureDirectory(),
                            FileName = "LocalCache.db",
                            OpenMode = SqliteOpenMode.ReadWriteCreate
                        };
                    }
                );
            }
        );

        return services;
    }

    /// <summary>
    /// Configure EasyCaching with Disk
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddEasyCachingDisk(this IServiceCollection services)
    {
        services.AddEasyCaching
        (
            options => {
                options.UseDisk
                (
                    diskOptions => {
                        diskOptions.EnableLogging = true;

                        diskOptions.DBConfig = new DiskDbOptions()
                        {
                            BasePath = "Storage/EasyCaching/Disk/".EnsureDirectory()
                        };
                    }
                );
            }
        );

        return services;
    }

    /// <summary>
    /// Configure EasyCaching on Run
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseEasyCaching(this IApplicationBuilder app)
    {
        var services = app.GetServiceProvider();

        // var cachingService = services.GetRequiredService<ICachingService>();

        // cachingService.InvalidateAll();

        return app;
    }
}