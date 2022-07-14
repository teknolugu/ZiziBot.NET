using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class InsightStatusCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public InsightStatusCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        _telegramService.InsightStatusMemberAsync().InBackground();
    }
}
