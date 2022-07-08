using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.GlobalBan;

public class GlobalBanCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public GlobalBanCommand(TelegramService telegramService)
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

        _telegramService.AddGlobalBanUserAsync().InBackground();
    }
}
