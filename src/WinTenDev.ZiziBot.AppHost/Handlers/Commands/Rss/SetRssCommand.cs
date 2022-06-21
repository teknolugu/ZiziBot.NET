using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class SetRssCommand : CommandBase
{
    private readonly RssService _rssService;
    private readonly RssFeedService _rssFeedService;
    private readonly TelegramService _telegramService;

    public SetRssCommand(
        TelegramService telegramService,
        RssService rssService,
        RssFeedService rssFeedService
    )
    {
        _telegramService = telegramService;
        _rssService = rssService;
        _rssFeedService = rssFeedService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        await _telegramService.AddRssUrlAsync();
    }
}
