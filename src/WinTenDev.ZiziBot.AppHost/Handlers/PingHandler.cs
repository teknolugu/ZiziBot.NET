using System;
using System.Threading;
using System.Threading.Tasks;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

internal class PingHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public PingHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        var op = Operation.Begin("Ping Command handler");

        await _telegramService.AddUpdateContext(context);

        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ping", "PONG"));

        var sendText = "ℹ️ Pong!!";
        var isSudoer = _telegramService.IsFromSudo;

        _telegramService.SaveSenderMessageToHistory(MessageFlag.Ping);

        if (_telegramService.IsPrivateChat && isSudoer)
        {
            sendText += $"\n🕔 <code>{DateTime.Now}</code>" +
                        "\n🎛 <b>Engine info.</b>";
            var getWebHookInfo = await _telegramService.Client.GetWebhookInfoAsync(cancellationToken: cancellationToken);
            if (string.IsNullOrEmpty(getWebHookInfo.Url))
            {
                sendText += "\n\n<i>Bot run in Poll mode.</i>";
            }
            else
            {
                sendText += getWebHookInfo.ParseWebHookInfo();
            }
        }

        await _telegramService.SendTextMessageAsync(sendText, keyboard);

        _telegramService.SaveSentMessageToHistory(MessageFlag.Ping);

        op.Complete();
    }
}