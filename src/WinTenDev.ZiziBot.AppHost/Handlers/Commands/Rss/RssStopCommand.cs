using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class RssStopCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly RssFeedService _rssFeedService;

    public RssStopCommand(
        TelegramService telegramService,
        RssFeedService rssFeedService
    )
    {
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

        await _telegramService.AppendTextAsync("Mematikan Job RSS..");
        var jobCount = _rssFeedService.UnRegisterRssFeedByChatId(chatId);

        await _telegramService.AppendTextAsync($"Sebanyak {jobCount} RSS berhasil dimatikan.");
    }
}