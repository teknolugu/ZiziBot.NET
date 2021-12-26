using System;
using BotFramework.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class MapConfigExtension
{
    [Obsolete("This function is Obsolete. Please use MappingAppSettings. Then use IOptionSnapshot<T>")]
    public static IServiceCollection AddMappingConfiguration(this IServiceCollection services)
    {
        Log.Information("Mapping configuration..");
        var serviceProvider = services.BuildServiceProvider();
        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        var config = serviceProvider.GetRequiredService<IConfiguration>();

        var appSettings = config.Get<AppConfig>();
        appSettings.EnvironmentConfig = new EnvironmentConfig()
        {
            HostEnvironment = env,
            IsDevelopment = env.IsDevelopment(),
            IsStaging = env.IsProduction(),
            IsProduction = env.IsProduction()
        };

        services.AddSingleton(appSettings);

        services.AddSingleton(appSettings.EnginesConfig);
        services.AddSingleton(appSettings.CommonConfig);
        // services.AddSingleton(appSettings.DatabaseConfig);
        services.AddSingleton(appSettings.HangfireConfig);
        services.AddSingleton(appSettings.ConnectionStrings);
        services.AddSingleton(appSettings.DataDogConfig);

        return services;
    }

    /// <summary>
    /// Load appsettings.json as snapshot POCO
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection MappingAppSettings(this IServiceCollection services)
    {
        Log.Information("Mapping configuration..");
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var env = serviceProvider.GetRequiredService<IHostEnvironment>();

        services.AddSingleton(new EnvironmentConfig()
        {
            HostEnvironment = env,
            IsDevelopment = env.IsDevelopment(),
            IsStaging = env.IsProduction(),
            IsProduction = env.IsProduction()
        });

        services.Configure<AllDebridConfig>(config.GetSection(nameof(AllDebridConfig)));
        services.Configure<BotConfig>(config.GetSection(nameof(BotConfig)));
        services.Configure<CacheConfig>(config.GetSection(nameof(CacheConfig)));
        services.Configure<CommonConfig>(config.GetSection(nameof(CommonConfig)));
        services.Configure<ConnectionStrings>(config.GetSection(nameof(ConnectionStrings)));
        services.Configure<DatabaseConfig>(config.GetSection(nameof(DatabaseConfig)));
        services.Configure<DataDogConfig>(config.GetSection(nameof(DataDogConfig)));
        services.Configure<EnginesConfig>(config.GetSection(nameof(EnginesConfig)));
        services.Configure<ExceptionlessConfig>(config.GetSection(nameof(ExceptionlessConfig)));
        services.Configure<EventLogConfig>(config.GetSection(nameof(EventLogConfig)));
        services.Configure<GoogleCloudConfig>(config.GetSection(nameof(GoogleCloudConfig)));
        services.Configure<GrafanaConfig>(config.GetSection(nameof(GrafanaConfig)));
        services.Configure<HangfireConfig>(config.GetSection(nameof(HangfireConfig)));
        services.Configure<SentryConfig>(config.GetSection(nameof(SentryConfig)));
        services.Configure<TdLibConfig>(config.GetSection(nameof(TdLibConfig)));
        services.Configure<SpamWatchConfig>(config.GetSection(nameof(SpamWatchConfig)));
        services.Configure<UptoboxConfig>(config.GetSection(nameof(UptoboxConfig)));
        services.Configure<RestrictionConfig>(config.GetSection(nameof(RestrictionConfig)));

        return services;
    }
}