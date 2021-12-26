using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Framework;

namespace WinTenDev.ZiziMirror.AppHost.Extensions
{
    public static class AppStartupExtension
    {
        public static IServiceCollection AddZiziBot(this IServiceCollection services)
        {
            var scope = services.BuildServiceProvider();
            var configuration = scope.GetRequiredService<IConfiguration>();

            // var botOptions = configuration.GetSection("ZiziBot").Get<BotOptions>();
            services
                // .AddTelegramBot<ZiziMirror>(botOptions);
                .AddTransient<ZiziMirror>()
                .Configure<BotOptions<ZiziMirror>>(configuration.GetSection(nameof(ZiziMirror)));
            // .Configure<CustomBotOptions<ZiziMirror>>(configuration.GetSection(nameof(ZiziMirror)));

            // services.AddScoped(service =>
            // {
            //     var botToken = configuration.GetValue<string>(nameof(ZiziMirror) + ":Token");
            //     return new TelegramBotClient(botToken);
            // });

            return services;
        }
    }
}