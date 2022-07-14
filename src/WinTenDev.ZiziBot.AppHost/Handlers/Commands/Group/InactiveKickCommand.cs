using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class InactiveKickCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public InactiveKickCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        _telegramService.InactiveKickMemberAsync().InBackground();
    }
}
