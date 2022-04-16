using System;
using System.Linq;
using Flurl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Nito.AsyncEx.Synchronous;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Framework;
using Telegram.Bot.Framework.Extensions;
using WinTenDev.Zizi.Models.Bots.Options;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
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

        if (tgBotConfig.UseLocalBotServer &&
            tgBotConfig.EngineMode == EngineMode.WebHook)
        {
            services.AddScoped<ITelegramBotClient>(
                _ => new TelegramBotClient(
                    token: tgBotConfig.ApiToken,
                    baseUrl: tgBotConfig.CustomBotServer
                )
            );
        }
        else
        {
            services.AddScoped<ITelegramBotClient>(_ => new TelegramBotClient(tgBotConfig.ApiToken));
        }

        return services;
    }

    public static IApplicationBuilder PrintAboutApp(this IApplicationBuilder app)
    {
        var engines = app.GetRequiredService<IOptionsSnapshot<EnginesConfig>>().Value;

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
        var tgBotConfig = app.GetRequiredService<IOptions<TgBotConfig>>().Value;
        var tunnelService = app.GetRequiredService<LocalTunnelService>();

        var configuration = app.GetRequiredService<IConfiguration>();

        Log.Information("Starting Bot in WebHook mode..");

        // use Telegram bot webhook middleware in higher environments
        app.UseTelegramBotWebhook<BotClient>(configureBot);

        if (tgBotConfig.EnableLocalTunnel)
        {
            var tunnelSubdomain = tgBotConfig.LocalTunnelSubdomain;

            var urlHost = configuration.GetValue<string>("Kestrel:Endpoints:Http:Url");
            var parted = urlHost.Split(":");
            var portHost = parted.ElementAtOrDefault(2);

            var tunnel = tunnelService.CreateTunnel(tunnelSubdomain, portHost);
            var tunnelUrl = tunnel.Information.Url;
            var webhookPath = tgBotConfig.WebhookPath;
            var webHookUrl = Url.Combine(tunnelUrl.ToString(), webhookPath);

            Log.Information("Setting WebHook to {TunnelUrl}", webHookUrl);

            var botClient = app.GetRequiredService<ITelegramBotClient>();
            botClient.DeleteWebhookAsync().WaitAndUnwrapException();
            botClient.SetWebhookAsync(webHookUrl).WaitAndUnwrapException();

            var webhookInfo = botClient.GetWebhookInfoAsync().WaitAndUnwrapException();
            Log.Information("Updated WebHook info {@WebhookInfo}", webhookInfo);
        }
        else
        {
            // and make sure webhook is enabled
            app.ApplicationServices.EnsureWebhookSet<BotClient>();
        }

        Log.Information("Bot is ready in WebHook mode..");

        return app;
    }

    public static IApplicationBuilder ExecuteStartupTasks(this IApplicationBuilder app)
    {
        var config = app.GetRequiredService<IConfiguration>();
        var botService = app.GetRequiredService<BotService>();

        app.GetRequiredService<DatabaseService>().FixTableCollation().WaitAndUnwrapException();

        botService.EnsureCommandRegistration().WaitAndUnwrapException();

        ChangeToken.OnChange(
            () => config.GetReloadToken(),
            () => botService.EnsureCommandRegistration().WaitAndUnwrapException()
        );

        return app;
    }
}