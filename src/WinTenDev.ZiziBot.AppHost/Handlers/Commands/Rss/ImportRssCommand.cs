using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class ImportRssCommand : CommandBase
{
    private readonly RssService _rssService;
    private readonly RssFeedService _rssFeedService;
    private readonly TelegramService _telegramService;

    public ImportRssCommand(
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

        var msg = _telegramService.Message;
        var msgId = msg.MessageId;
        var chatId = _telegramService.ChatId;
        var fromId = _telegramService.FromId;
        var dateDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (!await _telegramService.CheckUserPermission())
        {
            var send = "Maaf, hanya Admin yang dapat mengimport daftar RSS";
            await _telegramService.SendTextMessageAsync(send);
            return;
        }

        await _telegramService.AppendTextAsync("Sedang mempersiapkan");
        var filePath = $"{chatId}/rss-feed_{dateDate}_{msgId}";
        filePath = await _telegramService.DownloadFileAsync(filePath);

        await _telegramService.AppendTextAsync("Sedang membuka berkas");
        var rssLists = await File.ReadAllLinesAsync(filePath);

        foreach (var rssList in rssLists)
        {
            Log.Information("Importing {RssList}", rssList);

            var data = new Dictionary<string, object>()
            {
                { "url_feed", rssList },
                { "chat_id", chatId },
                { "from_id", fromId }
            };

            await _rssService.SaveRssSettingAsync(data);
        }

        await _telegramService.AppendTextAsync($"Memeriksa RSS duplikat");
        var dedupe = await _rssService.DeleteDuplicateAsync();

        await _telegramService.AppendTextAsync("Memastikan RSS Scheduler berjalan");
        _rssFeedService.ReRegisterRssFeedByChatId(chatId);

        var importCount = rssLists.Length;

        if (dedupe != importCount)
        {
            var diff = importCount - dedupe;
            await _telegramService.AppendTextAsync($"{diff} RSS berhasil di import");
        }
        else
        {
            await _telegramService.AppendTextAsync($"RSS telah di import");
        }
    }
}