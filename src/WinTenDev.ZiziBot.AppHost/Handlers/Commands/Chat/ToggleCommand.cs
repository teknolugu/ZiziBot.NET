using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Chat;

public class ToggleCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public ToggleCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.SaveSettingToggleInCommandAsync();
    }
}
