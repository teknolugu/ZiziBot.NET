using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class AboutCommand : CommandBase
{
    private readonly BotService _botService;
    private readonly TelegramService _telegramService;
    private readonly EnginesConfig _enginesConfig;

    public AboutCommand(
        IOptionsSnapshot<EnginesConfig> enginesConfig,
        BotService botService,
        TelegramService telegramService
    )
    {
        _enginesConfig = enginesConfig.Value;
        _botService = botService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.SendAboutAsync();
    }
}