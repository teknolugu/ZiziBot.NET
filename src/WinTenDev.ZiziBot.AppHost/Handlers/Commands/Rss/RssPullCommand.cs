using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class RssPullCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly JobsService _jobsService;

    public RssPullCommand(
        TelegramService telegramService,
        JobsService jobsService
    )
    {
        _telegramService = telegramService;
        _jobsService = jobsService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        await _telegramService.RssPullAsync();
    }
}
