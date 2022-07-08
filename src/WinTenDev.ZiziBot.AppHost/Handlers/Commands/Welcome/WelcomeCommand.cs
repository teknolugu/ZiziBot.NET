using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;

public class WelcomeCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public WelcomeCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        _telegramService.GetWelcomeSettingsAsync().InBackground();
    }
}
