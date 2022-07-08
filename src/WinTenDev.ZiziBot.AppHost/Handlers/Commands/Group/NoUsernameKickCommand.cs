using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class NoUsernameKickCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public NoUsernameKickCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        _telegramService.NoUsernameKickMemberAsync().InBackground();
    }
}
