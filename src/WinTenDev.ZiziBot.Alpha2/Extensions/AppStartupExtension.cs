using DotNurse.Injector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TgBotFramework;

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
                        .Use<NewUpdateHandler>()
                        .UseWhen<PingHandler>(When.PingReceived)
                        .UseCommand<AddNoteCommand>("add_note")
                        .UseCommand<NotesCommand>("notes")
                        .UseCommand<StartCommand>("start")
                        .Use<GenericMessageHandler>()
                )
        );

        services.AddSingleton<ITelegramBotClient>(
            provider =>
                new TelegramBotClient(tgBotConfig.ApiToken)
        );

        services.AddServicesFrom(
            type =>
                type.Namespace?.Contains("WinTenDev.ZiziBot.Alpha2.Handlers") ?? false,
            ServiceLifetime.Scoped
        );

        return services;
    }
}
