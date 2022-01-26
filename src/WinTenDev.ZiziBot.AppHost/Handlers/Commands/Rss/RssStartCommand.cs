using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class RssStartCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly RssService _rssService;
    private readonly RssFeedService _rssFeedService;

    public RssStartCommand(
        TelegramService telegramService,
        RssService rssService,
        RssFeedService rssFeedService
    )
    {
        _rssService = rssService;
        _rssFeedService = rssFeedService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var chatId = _telegramService.ChatId;

        if (!await _telegramService.CheckUserPermission())
        {
            Log.Warning("This command only for sudo!");
            return;
        }

        await _telegramService.AppendTextAsync("Memulai Job RSS..");

        _rssFeedService.UnRegisterRssFeedByChatId(chatId);

        var rssSettings = await _rssService.GetRssSettingsAsync(chatId);
        var rssCount = rssSettings.Count();

        foreach (var rssSetting in rssSettings)
        {
            var urlFeed = rssSetting.UrlFeed;

            _rssFeedService.RegisterUrlFeed(chatId, urlFeed);
        }

        await _telegramService.AppendTextAsync($"Sebanyak {rssCount} RSS berhasil dimulai");
    }
}