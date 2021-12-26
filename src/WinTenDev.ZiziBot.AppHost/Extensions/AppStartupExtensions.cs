using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Framework;
using Telegram.Bot.Framework.Extensions;
using WinTenDev.Zizi.Models.Bots.Options;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Extensions;

internal static class AppStartupExtensions
{
    public static IServiceCollection AddZiziBot(this IServiceCollection services)
    {
        var scope = services.BuildServiceProvider();
        var configuration = scope.GetRequiredService<IConfiguration>();

        services
            .AddTransient<ZiziBot>()
            .Configure<BotOptions<ZiziBot>>(configuration.GetSection("ZiziBot"))
            .Configure<CustomBotOptions<ZiziBot>>(configuration.GetSection("ZiziBot"));

        services.AddScoped(service => {
            var botToken = configuration.GetValue<string>("ZiziBot:ApiToken");
            return new TelegramBotClient(botToken);
        });

        return services;
    }

    public static IApplicationBuilder AboutApp(this IApplicationBuilder app)
    {
        var engines = app.GetServiceProvider().GetRequiredService<IOptionsSnapshot<EnginesConfig>>().Value;

        Log.Information("Name: {ProductName}", engines.ProductName);
        Log.Information("Version: {Version}", engines.Version);
        Log.Information("Company: {Company}", engines.Company);

        return app;
    }

    public static IApplicationBuilder RunZiziBot(this IApplicationBuilder app)
    {
        Log.Information("Starting run ZiziBot..");
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var serviceProvider = app.GetServiceProvider();
        var commonConfig = serviceProvider.GetRequiredService<IOptionsSnapshot<CommonConfig>>().Value;

        switch (commonConfig.EngineMode)
        {
            case EngineMode.Polling:
                app.RunInPooling();
                break;
            case EngineMode.WebHook:
                app.RunInWebHook();
                break;
            case EngineMode.FollowHost:
            {
                if (env.IsDevelopment())
                {
                    app.RunInPooling();
                }
                else
                {
                    app.RunInWebHook();
                }

                break;
            }
            default:
                Log.Error("Unknown Engine Mode!");
                break;
        }

        return app;
    }

    private static IApplicationBuilder RunInPooling(this IApplicationBuilder app)
    {
        var configureBot = CommandBuilderExtension.ConfigureBot();

        Log.Information("Starting ZiziBot in Pooling mode..");

        // get bot updates from Telegram via long-polling approach during development
        // this will disable Telegram webhooks
        app.UseTelegramBotLongPolling<ZiziBot>(configureBot, TimeSpan.FromSeconds(1));

        Log.Information("ZiziBot is ready in Pooling mode..");

        return app;
    }

    private static IApplicationBuilder RunInWebHook(this IApplicationBuilder app)
    {
        var configureBot = CommandBuilderExtension.ConfigureBot();

        // app.UseLocalTunnel("zizibot-localwebhook");

        Log.Information("Starting ZiziBot in WebHook mode..");
        // use Telegram bot webhook middleware in higher environments
        app.UseTelegramBotWebhook<ZiziBot>(configureBot);

        // and make sure webhook is enabled
        app.ApplicationServices.EnsureWebhookSet<ZiziBot>();

        Log.Information("ZiziBot is ready in WebHook mode..");

        return app;
    }
}