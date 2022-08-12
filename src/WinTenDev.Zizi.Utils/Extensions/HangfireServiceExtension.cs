using System;
using Hangfire;
using Hangfire.Dashboard.Dark;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.MemoryStorage;
using Hangfire.Server;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nito.AsyncEx.Synchronous;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Interfaces;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class HangfireServiceExtension
{
    public static IServiceCollection AddHangfireServerAndConfig(this IServiceCollection services)
    {
        Log.Debug("Adding Hangfire Service");

        var scope = services.BuildServiceProvider();

        var hangfireConfig = scope.GetRequiredService<IOptionsSnapshot<HangfireConfig>>().Value;
        var connStrings = scope.GetRequiredService<IOptionsSnapshot<ConnectionStrings>>().Value;

        services.AddHangfire(
            config => {
                // switch (hangfireConfig.DataStore)
                // {
                //     case HangfireDataStore.MySql:
                //         var hangfireMysql = hangfireConfig.MysqlConnection;
                //         if (hangfireMysql.IsNullOrEmpty()) hangfireMysql = connStrings.MySql;
                //
                //         // config.UseStorage(HangfireUtil.GetMysqlStorage(hangfireMysql));
                //         JobStorage.Current = HangfireUtil.GetMysqlStorage(hangfireMysql);
                //         break;
                //
                // case HangfireDataStore.Sqlite:
                //         // config.UseStorage(HangfireUtil.GetSqliteStorage(hangfireConfig.SqliteConnection));
                //         JobStorage.Current = HangfireUtil.GetSqliteStorage(hangfireConfig.SqliteConnection);
                //         break;
                //
                // case HangfireDataStore.Litedb:
                //         // config.UseStorage(HangfireUtil.GetLiteDbStorage(hangfireConfig.LiteDbConnection));
                //         JobStorage.Current = HangfireUtil.GetLiteDbStorage(hangfireConfig.LiteDbConnection);
                //         break;
                //
                //     case HangfireDataStore.Redis:
                //         // config.UseStorage(HangfireUtil.GetRedisStorage(hangfireConfig.RedisConnection));
                //         // config.UseRedisStorage(HangfireUtil.GetRedisConnectionMultiplexer(hangfireConfig.RedisConnection));
                //         JobStorage.Current = HangfireUtil.GetRedisStorage(hangfireConfig.RedisConnection);
                //         break;
                //
                //     case HangfireDataStore.Memory:
                //         // config.UseMemoryStorage();
                //         JobStorage.Current = new MemoryStorage();
                //         break;
                //
                //     default:
                //         Log.Warning("Unknown Hangfire DataStore");
                //         break;
                // }

                config.UseDarkDashboard()
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseHeartbeatPage(TimeSpan.FromSeconds(15))
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseColouredConsoleLogProvider()
                    .UseSerilogLogProvider();
            }
        );

        switch (hangfireConfig.DataStore)
        {
            case HangfireDataStore.MySql:
                var hangfireMysql = hangfireConfig.MysqlConnection;
                if (hangfireMysql.IsNullOrEmpty()) hangfireMysql = connStrings.MySql;
                JobStorage.Current = HangfireUtil.GetMysqlStorage(hangfireMysql);
                break;

            case HangfireDataStore.Sqlite:
                JobStorage.Current = HangfireUtil.GetSqliteStorage(hangfireConfig.SqliteConnection);
                break;

            case HangfireDataStore.Litedb:
                JobStorage.Current = HangfireUtil.GetLiteDbStorage(hangfireConfig.LiteDbConnection);
                break;

            case HangfireDataStore.Redis:
                JobStorage.Current = HangfireUtil.GetRedisStorage(hangfireConfig.RedisConnection);
                break;

            case HangfireDataStore.MongoDb:
                JobStorage.Current = HangfireUtil.GetMongoDbStorage(hangfireConfig.MongoDbConnection);
                break;

            default:
                JobStorage.Current = new MemoryStorage();
                Log.Warning("Set default Hangfire Storage to MemoryStorage");
                break;
        }

        services.AddHangfireServer(
            (
                provider,
                options
            ) => {
                options.WorkerCount = Environment.ProcessorCount * hangfireConfig.WorkerMultiplier;
                options.Queues = hangfireConfig.Queues;
            },
            storage: JobStorage.Current,
            additionalProcesses: new IBackgroundProcess[]
            {
                new ProcessMonitor(TimeSpan.FromSeconds(3))
            });

        Log.Debug("Hangfire Service added..");

        return services;
    }

    public static IApplicationBuilder ResetHangfireStorageIfRequired(this IApplicationBuilder app)
    {
        var serviceProvider = app.GetServiceProvider();

        var storageService = serviceProvider.GetRequiredService<IStorageService>();
        var parseError = ErrorUtil.ParseErrorTextAsync().Result;
        var lastFtlError = parseError.FullText;

        if (lastFtlError.Contains("hangfire", StringComparison.CurrentCultureIgnoreCase))
        {
            if (lastFtlError.Contains("Storage", StringComparison.CurrentCultureIgnoreCase))
            {
                Log.Warning("Last error about Hangfire, seem need to Reset MySql Storage");
                storageService.ResetHangfireMySqlStorage().WaitAndUnwrapException();

                "".SaveErrorToText().WaitAndUnwrapException();
            }
        }

        return app;
    }

    public static IApplicationBuilder UseHangfireDashboardAndServer(this IApplicationBuilder app)
    {
        var serviceProvider = app.GetServiceProvider();
        var hangfireConfig = serviceProvider.GetRequiredService<IOptionsSnapshot<HangfireConfig>>().Value;
        var env = serviceProvider.GetRequiredService<IHostEnvironment>();
        var server = serviceProvider.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;


        var baseUrl = hangfireConfig.BaseUrl;
        var username = hangfireConfig.Username;
        var password = hangfireConfig.Password;

        Log.Information("Hangfire Url: {HangfireBaseUrl}", baseUrl);
        Log.Information(
            "Hangfire Auth: {HangfireUsername} | {HangfirePassword}",
            username,
            password
        );

        app.ResetHangfireStorageIfRequired();

        var dashboardOptions = new DashboardOptions();

        if (!env.IsDevelopment())
        {
            dashboardOptions.Authorization = new[]
            {
                new HangfireCustomBasicAuthenticationFilter
                {
                    User = username, Pass = password
                }
            };
        }

        app.UseHangfireDashboard(baseUrl, dashboardOptions);

        // var serverOptions = new BackgroundJobServerOptions
        // {
        //     WorkerCount = Environment.ProcessorCount * hangfireConfig.WorkerMultiplier,
        //     Queues = hangfireConfig.Queues
        // };
        //
        // app.UseHangfireServer(
        //     serverOptions,
        //     new[]
        //     {
        //         new ProcessMonitor(TimeSpan.FromSeconds(3))
        //     }
        // );

        Log.Information("Hangfire is Running..");
        return app;
    }

}