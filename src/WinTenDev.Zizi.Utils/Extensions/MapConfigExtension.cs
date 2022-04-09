using System;
using System.Linq;
using BotFramework.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MoreLinq;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils.IO;

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

    public static IConfigurationBuilder AddAppSettingsJson(this IConfigurationBuilder builder)
    {
        var configDir = "Storage/AppSettings/Current/".EnsureDirectory();
        var listConfigAll = configDir.EnumerateFiles();

        // If json file name ends with 'x.json',
        // it not will be added because marked as disabled
        var listEnabledConfig = listConfigAll
            .Where(s => !s.EndsWith("x.json"));

        listEnabledConfig.ForEach
        (
            (filePath) => builder.AddJsonFile(
                filePath,
                true,
                true
            )
        );

        return builder;
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

        services.Configure<AllDebridConfig>(config.GetSection(nameof(AllDebridConfig)));
        services.Configure<BinderByteConfig>(config.GetSection(nameof(BinderByteConfig)));
        services.Configure<BotConfig>(config.GetSection(nameof(BotConfig)));
        services.Configure<ButtonConfig>(config.GetSection(nameof(ButtonConfig)));
        services.Configure<CacheConfig>(config.GetSection(nameof(CacheConfig)));
        services.Configure<CommandConfig>(config.GetSection(nameof(CommandConfig)));
        services.Configure<CommonConfig>(config.GetSection(nameof(CommonConfig)));
        services.Configure<ConnectionStrings>(config.GetSection(nameof(ConnectionStrings)));
        services.Configure<DatabaseConfig>(config.GetSection(nameof(DatabaseConfig)));
        services.Configure<DataDogConfig>(config.GetSection(nameof(DataDogConfig)));
        services.Configure<EnginesConfig>(config.GetSection(nameof(EnginesConfig)));
        services.Configure<ExceptionlessConfig>(config.GetSection(nameof(ExceptionlessConfig)));
        services.Configure<EventLogConfig>(config.GetSection(nameof(EventLogConfig)));
        services.Configure<FeatureConfig>(config.GetSection(nameof(FeatureConfig)));
        services.Configure<GoogleCloudConfig>(config.GetSection(nameof(GoogleCloudConfig)));
        services.Configure<GrafanaConfig>(config.GetSection(nameof(GrafanaConfig)));
        services.Configure<HangfireConfig>(config.GetSection(nameof(HangfireConfig)));
        services.Configure<HealthConfig>(config.GetSection(nameof(HealthConfig)));
        services.Configure<LocalizationConfig>(config.GetSection(nameof(LocalizationConfig)));
        services.Configure<NewRelicConfig>(config.GetSection(nameof(NewRelicConfig)));
        services.Configure<OctokitConfig>(config.GetSection(nameof(OctokitConfig)));
        services.Configure<OptiicDevConfig>(config.GetSection(nameof(OptiicDevConfig)));
        services.Configure<SentryConfig>(config.GetSection(nameof(SentryConfig)));
        services.Configure<TdLibConfig>(config.GetSection(nameof(TdLibConfig)));
        services.Configure<TgBotConfig>(config.GetSection(nameof(TgBotConfig)));
        services.Configure<SpamWatchConfig>(config.GetSection(nameof(SpamWatchConfig)));
        services.Configure<UptoboxConfig>(config.GetSection(nameof(UptoboxConfig)));
        services.Configure<RestrictionConfig>(config.GetSection(nameof(RestrictionConfig)));

        return services;
    }
}