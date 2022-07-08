using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class DelRssCommand : CommandBase
{
    private readonly RssService _rssService;
    private readonly RssFeedService _rssFeedService;
    private readonly TelegramService _telegramService;

    public DelRssCommand(
        RssService rssService,
        RssFeedService rssFeedService,
        TelegramService telegramService
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

        var checkUserPermission = await _telegramService.CheckUserPermission();
        if (!checkUserPermission)
        {
            Log.Warning("Delete RSS only for admin or private chat!");
            await _telegramService.DeleteAsync();
            return;
        }

        var urlFeed = _telegramService.Message.Text.GetTextWithoutCmd();

        await _telegramService.SendTextMessageAsync($"Sedang menghapus {urlFeed}");

        var delete = await _rssService.DeleteRssAsync(chatId, urlFeed);

        var success = delete.ToBool()
            ? "berhasil."
            : "gagal. Mungkin RSS tersebut sudah di hapus atau belum di tambahkan";

        await _telegramService.EditMessageTextAsync($"Hapus {urlFeed} {success}");

        await _rssFeedService.ReRegisterRssFeedByChatId(chatId);
    }
}
