using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Callbacks;

public class RssCtlCallback
{
    private readonly TelegramService _telegramService;
    private readonly MessageHistoryService _messageHistoryService;
    private readonly RssFeedService _rssFeedService;
    private readonly RssService _rssService;

    public RssCtlCallback(
        TelegramService telegramService,
        MessageHistoryService messageHistoryService,
        RssFeedService rssFeedService,
        RssService rssService
    )
    {
        _telegramService = telegramService;
        _messageHistoryService = messageHistoryService;
        _rssFeedService = rssFeedService;
        _rssService = rssService;
    }

    public async Task<bool> ExecuteAsync()
    {
        var chatId = _telegramService.ChatId;
        var chatTitle = _telegramService.ChatTitle;
        var messageId = _telegramService.CallBackMessageId;
        var answerHeader = $"RSS Control for {chatTitle}";
        var answerDescription = string.Empty;
        var part = _telegramService.CallbackQuery.Data?.Split(" ");
        var rssId = part!.ElementAtOrDefault(2);
        var page = 0;
        const int take = 5;

        if (!await _telegramService.CheckUserPermission())
        {
            await _telegramService.AnswerCallbackQueryAsync("Anda tidak mempunyai akses", true);

            return false;
        }

        var rssFind = new RssSettingFindDto()
        {
            ChatId = chatId
        };

        var updateValue = new Dictionary<string, object>()
        {
            { "updated_at", DateTime.UtcNow }
        };

        switch (part.ElementAtOrDefault(1))
        {
            case "stop-all":
                updateValue.Add("is_enabled", false);
                answerDescription = $"Semua service berhasil dimatikan";
                break;

            case "start-all":
                updateValue.Add("is_enabled", true);
                answerDescription = "Semua service berhasil diaktifkan";
                break;

            case "start":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("is_enabled", true);
                answerDescription = "Service berhasil di aktifkan";
                break;

            case "stop":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("is_enabled", false);
                answerDescription = "Service berhasil dimatikan";
                break;

            case "attachment-off":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("include_attachment", false);
                answerDescription = "Attachment tidak akan ditambahkan";
                break;

            case "attachment-on":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("include_attachment", true);
                answerDescription = "Berhasil mengaktifkan attachment";
                break;

            case "delete":
                await _rssService.DeleteRssAsync(
                    chatId: chatId,
                    columnName: "id",
                    columnValue: rssId
                );
                answerDescription = "Service berhasil dihapus";
                break;

            case "navigate-page":
                var toPage = part.ElementAtOrDefault(2).ToInt();
                page = toPage;
                answerDescription = "Halaman " + (toPage + 1);
                break;
        }

        await _rssService.UpdateRssSettingAsync(rssFind, updateValue);

        await _rssFeedService.ReRegisterRssFeedByChatId(chatId);

        var answerCombined = answerHeader + Environment.NewLine + answerDescription;

        var btnMarkupCtl = await _rssService.GetButtonMarkup(
            chatId: chatId,
            page: page,
            take: take
        );

        if (answerDescription.IsNotNullOrEmpty())
        {
            await _telegramService.EditMessageCallback(answerCombined, btnMarkupCtl);

            if (part.ElementAtOrDefault(1)?.Contains("all") ?? false)
                await _telegramService.AnswerCallbackQueryAsync(answerCombined, true);
        }

        await _messageHistoryService.UpdateDeleteAtAsync(
            new MessageHistoryFindDto()
            {
                ChatId = chatId,
                MessageId = messageId
            },
            DateTime.UtcNow.AddMinutes(10)
        );

        return true;
    }
}
