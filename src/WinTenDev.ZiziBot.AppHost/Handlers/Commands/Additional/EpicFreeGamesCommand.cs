using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class EpicFreeGamesCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly EpicGamesService _epicGamesService;

    public EpicFreeGamesCommand(
        TelegramService telegramService,
        EpicGamesService epicGamesService
    )
    {
        _telegramService = telegramService;
        _epicGamesService = epicGamesService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        await _telegramService.GetEpicGamesFreeAsync();
    }
}