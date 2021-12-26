using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Services.Internals;

namespace WinTenDev.Zizi.Services.Telegram;

public class BotService
{
    private readonly ILogger<BotService> _logger;
    private readonly TelegramBotClient _botClient;
    private readonly CacheService _cacheService;

    public BotService(
        ILogger<BotService> logger,
        TelegramBotClient botClient,
        CacheService cacheService
    )
    {
        _logger = logger;
        _botClient = botClient;
        _cacheService = cacheService;
    }

    public async Task<User> GetMeAsync()
    {
        var getMe = await _cacheService.GetOrSetAsync("get-me", async () => {
            var getMe = await _botClient.GetMeAsync();
            return getMe;
        });

        return getMe;
    }

    public async Task<bool> IsBeta()
    {
        var me = await GetMeAsync();
        var isBeta = me.Username?.Contains("beta", StringComparison.OrdinalIgnoreCase) ?? false;
        _logger.LogInformation("Is Bot {Me} IsBeta: {IsBeta}", me, isBeta);

        return isBeta;
    }

    public async Task<string> GetUrlStart(string param)
    {
        var getMe = await GetMeAsync();
        var username = getMe.Username;
        var urlStart = $"https://t.me/{username}?{param}";

        return urlStart;
    }
}