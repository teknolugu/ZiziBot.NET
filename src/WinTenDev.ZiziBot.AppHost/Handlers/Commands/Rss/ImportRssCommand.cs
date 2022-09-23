using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class ImportRssCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public ImportRssCommand(
        TelegramService telegramService
    )
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

        await _telegramService.ImportRssAsync();
    }
}