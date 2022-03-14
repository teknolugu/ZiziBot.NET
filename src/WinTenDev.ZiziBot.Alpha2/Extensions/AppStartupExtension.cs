using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TgBotFramework;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.ZiziBot.Alpha2.Extensions;

public static class AppStartupExtension
{
    public static IServiceCollection AddTelegramBot(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var tgBotConfig = serviceProvider.GetRequiredService<IOptions<TgBotConfig>>().Value;

        services.AddBotService<UpdateContext>(
            botToken: tgBotConfig.ApiToken,
            configure: builder => builder
                .UseLongPolling()
                .SetPipeline(
                    pipeBuilder => pipeBuilder
                        .UseCommand<StartCommand>("start")
                )
        );

        services.AddSingleton<StartCommand>();

        return services;
    }
}