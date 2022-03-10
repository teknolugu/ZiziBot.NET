using System;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class RssCtlCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly RssService _rssService;

    public RssCtlCommand(
        TelegramService telegramService,
        RssService rssService
    )
    {
        _telegramService = telegramService;
        _rssService = rssService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);
        var chatId = _telegramService.ChatId;
        var chatTitle = _telegramService.ChatTitle;

        if (!await _telegramService.CheckUserPermission())
        {
            await _telegramService.DeleteSenderMessageAsync();
            return;
        }

        await _telegramService.SendTextMessageAsync("Sedang mengambil RSS..", replyToMsgId: 0);

        var buttonMarkup = await _rssService.GetButtonMarkup(chatId);

        var messageText = buttonMarkup == null
            ? "Sepertinya tidak ada RSS di obrolan ini"
            : $"RSS Control for {chatTitle}" +
              "\nHalaman 1";

        await _telegramService.EditMessageTextAsync(
            new MessageResponseDto()
            {
                MessageText = messageText,
                ReplyMarkup = buttonMarkup,
                ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(10),
                IncludeSenderForDelete = true
            }
        );
    }
}