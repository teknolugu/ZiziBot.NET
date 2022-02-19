using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx.Synchronous;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils.IO;
using WTelegram;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class TelegramServiceExtension
{
    public static IServiceCollection AddWtTelegramApi(this IServiceCollection services)
    {
        services.AddSingleton
        (
            provider => {
                var serviceScope = provider.CreateScope().ServiceProvider;
                var tdLibConfig = serviceScope.GetRequiredService<IOptionsSnapshot<TdLibConfig>>().Value;
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("WTelegram");

                var apiId = tdLibConfig.ApiId;
                var sessionFile = $"Storage/Common/WTelegram_session_{apiId}.dat".EnsureDirectory();

                string Config(string what)
                {
                    switch (what)
                    {
                        case "api_id": return apiId;
                        case "api_hash": return tdLibConfig.ApiHash;
                        case "phone_number": return tdLibConfig.PhoneNumber;
                        case "verification_code":
                        {
                            logger.LogInformation("Input Verification Code: ");
                            return Console.ReadLine();
                        }
                        case "session_pathname": return sessionFile;
                        case "first_name": return tdLibConfig.FirstName;// if sign-up is required
                        case "last_name": return tdLibConfig.LastName;// if sign-up is required
                        // case "password": return "secret!";     // if user has enabled 2FA
                        default: return null;// let WTelegramClient decide the default config
                    }
                }

                Helpers.Log = (
                    logLevel,
                    logStr
                ) => logger.Log((LogLevel) logLevel, "WTelegram: {S}", logStr);

                var client = new Client(Config);
                var user = client.LoginUserIfNeeded().WaitAndUnwrapException();

                logger.LogInformation
                (
                    "We are logged-in as {0} (id {1})",
                    user.username ?? user.first_name + " " + user.last_name, user.id
                );

                return client;
            }
        );

        return services;
    }
}