using BotFramework.Config;
using DotNurse.Injector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace WinTenDev.ZiziBot.Alpha1.Extensions
{
    /// <summary>
    /// Common Service extension is contain common service
    /// </summary>
    public static class CommonServiceExtension
    {
        /// <summary>
        /// Add common service
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
        {
            services.AddScoped(
                service => {
                    var botConfig = service.GetRequiredService<IOptions<BotConfig>>().Value;
                    var client = new TelegramBotClient(botConfig.Token);

                    return client;
                }
            );

            services.AddServicesFrom("WinTenDev.ZiziBot.Alpha1.Handlers.Modules", ServiceLifetime.Scoped);
            services.AddServicesFrom("WinTenDev.ZiziBot.Alpha1.Handlers.CallbackHandlers", ServiceLifetime.Scoped);

            return services;
        }
    }
}