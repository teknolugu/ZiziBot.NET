using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

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

        var chatId = _telegramService.ChatId;
        var fromId = _telegramService.FromId;

        var checkUserPermission = await _telegramService.CheckUserPermission();

        if (!checkUserPermission)
        {
            Log.Warning("Modify RSS only for admin or private chat!");
            await _telegramService.DeleteSenderMessageAsync();
            return;
        }

        var url = _telegramService.Message.Text.GetTextWithoutCmd();

        if (url.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync("Apa url Feednya?");
            return;
        }

        await _telegramService.AppendTextAsync($"URL: {url}");

        if (!url.CheckUrlValid())
        {
            await _telegramService.AppendTextAsync("Url tersebut sepertinya tidak valid");
            return;
        }

        await _telegramService.AppendTextAsync($"Memeriksa RSS Feed");

        var isValid = await url.IsValidUrlFeed();

        if (!isValid)
        {
            var baseUrl = url.GetBaseUrl();

            await _telegramService.AppendTextAsync("Mencari kemungkinan RSS Feed yang valid");
            var foundUrl = await baseUrl.FindUrlFeed();

            Log.Debug("Found URL Feed: {FoundUrl}", foundUrl);

            if (foundUrl != "")
            {
                await _telegramService.AppendTextAsync("Menemukan: " + foundUrl);
                url = foundUrl;
            }
            else
            {
                var notFoundRss = $"Kami tidak dapat memvalidasi {url} adalah Link RSS yang valid, " +
                                  $"dan mencoba mencari di {baseUrl} tetap tidak dapat menemukan.";

                await _telegramService.EditMessageTextAsync(notFoundRss);
                return;
            }
        }

        var isFeedExist = await _rssService.IsRssExist(chatId, url);

        Log.Information("Is Url Exist: {IsFeedExist}", isFeedExist);

        if (!isFeedExist)
        {
            await _telegramService.AppendTextAsync($"Sedang menyimpan..");

            var data = new Dictionary<string, object>()
            {
                { "url_feed", url },
                { "chat_id", chatId },
                { "from_id", fromId }
            };

            await _rssService.SaveRssSettingAsync(data);

            await _telegramService.AppendTextAsync("Memastikan Scheduler sudah berjalan");

            _rssFeedService.RegisterUrlFeed(chatId, url);

            await _telegramService.AppendTextAsync($"Tautan berhasil di simpan");
        }
        else
        {
            await _telegramService.AppendTextAsync($"Tautan sudah di simpan");
        }
    }
}