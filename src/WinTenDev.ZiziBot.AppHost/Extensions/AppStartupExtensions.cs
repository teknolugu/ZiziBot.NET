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
    public static IServiceCollection AddTelegramBot(this IServiceCollection services)
    {
        var scope = services.BuildServiceProvider();
        var configuration = scope.GetRequiredService<IConfiguration>();
        var configSection = configuration.GetSection(nameof(TgBotConfig));
        var tgBotConfig = scope.GetRequiredService<IOptions<TgBotConfig>>().Value;

        services
            .AddTransient<BotClient>()
            .Configure<BotOptions<BotClient>>(configSection)
            .Configure<CustomBotOptions<BotClient>>(configSection);

        services.AddScoped<ITelegramBotClient>(_ => new TelegramBotClient(tgBotConfig.ApiToken));

        return services;
    }

    public static IApplicationBuilder PrintAboutApp(this IApplicationBuilder app)
    {
        var engines = app.GetServiceProvider()
            .GetRequiredService<IOptionsSnapshot<EnginesConfig>>().Value;

        Log.Information("Name: {ProductName}", engines.ProductName);
        Log.Information("Version: {Version}", engines.Version);
        Log.Information("Company: {Company}", engines.Company);

        return app;
    }

    public static IApplicationBuilder RunTelegramBot(this IApplicationBuilder app)
    {
        Log.Information("Starting TelegramBot Client..");
        var services = app.GetServiceProvider();
        var currentEnv = services.GetRequiredService<IWebHostEnvironment>();
        var tgBotConfig = services.GetRequiredService<IOptions<TgBotConfig>>().Value;

        switch (tgBotConfig.EngineMode)
        {
            case EngineMode.Polling:
                app.UseTelegramBotPoolingMode();
                break;
            case EngineMode.WebHook:
                app.UseTelegramBotWebHookMode();
                break;
            case EngineMode.Environment:
            {
                if (currentEnv.IsDevelopment())
                {
                    app.UseTelegramBotPoolingMode();
                }
                else
                {
                    app.UseTelegramBotWebHookMode();
                }

                break;
            }
            default:
                Log.Error("Unknown Engine Mode!");
                break;
        }

        return app;
    }

    private static IApplicationBuilder UseTelegramBotPoolingMode(this IApplicationBuilder app)
    {
        var configureBot = CommandBuilderExtension.ConfigureBot();

        Log.Information("Starting Bot in Pooling mode..");

        // get bot updates from Telegram via long-polling approach during development
        // this will disable Telegram webhooks
        app.UseTelegramBotLongPolling<BotClient>(configureBot, TimeSpan.FromSeconds(1));

        Log.Information("Bot is ready in Pooling mode..");

        return app;
    }

    private static IApplicationBuilder UseTelegramBotWebHookMode(this IApplicationBuilder app)
    {
        var configureBot = CommandBuilderExtension.ConfigureBot();

        // app.UseLocalTunnel("zizibot-dev-localwebhook");

        Log.Information("Starting Bot in WebHook mode..");
        // use Telegram bot webhook middleware in higher environments
        app.UseTelegramBotWebhook<BotClient>(configureBot);

        // and make sure webhook is enabled
        app.ApplicationServices.EnsureWebhookSet<BotClient>();

        Log.Information("Bot is ready in WebHook mode..");

        return app;
    }
}