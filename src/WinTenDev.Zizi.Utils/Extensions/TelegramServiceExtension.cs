using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nito.AsyncEx.Synchronous;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WTelegram;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class TelegramServiceExtension
{
    public static IServiceCollection AddWtTelegramApi(this IServiceCollection services)
    {
        services.AddSingleton(provider => {
            var serviceScope = provider.CreateScope().ServiceProvider;
            var _tdLibConfig = serviceScope.GetRequiredService<IOptionsSnapshot<TdLibConfig>>().Value;
            var _logger = serviceScope.GetRequiredService<ILogger>();

            string Config(string what)
            {
                switch (what)
                {
                    case "api_id": return _tdLibConfig.ApiId;
                    case "api_hash": return _tdLibConfig.ApiHash;
                    case "phone_number": return _tdLibConfig.PhoneNumber;
                    case "verification_code":
                    {
                        _logger.Information("Input Verification Code: ");
                        return Console.ReadLine();
                    }
                    case "session_pathname": return "Storage/Common/WTelegram_session.dat";
                    case "first_name": return _tdLibConfig.FirstName;// if sign-up is required
                    case "last_name": return _tdLibConfig.LastName;// if sign-up is required
                    // case "password": return "secret!";     // if user has enabled 2FA
                    default: return null;// let WTelegramClient decide the default config
                }
            }

            Helpers.Log = (i, s) => _logger.Debug("WTelegram: {I} - {S}", i, s);

            var client = new Client(Config);
            var user = client.LoginUserIfNeeded().WaitAndUnwrapException();
            _logger.Information("We are logged-in as {0} (id {1})",
            user.username ?? user.first_name + " " + user.last_name, user.id);

            return client;
        });

        return services;
    }
}