using System;
using System.Threading;
using System.Threading.Tasks;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Telegram;

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
        var senderMessageId = _telegramService.MessageOrEdited.MessageId;

        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData(
                "Ping",
                "PONG"
            )
        );

        var sendText = "ℹ️ Pong!!";
        var isSudoer = _telegramService.IsFromSudo;

        await _telegramService.SaveToMessageHistoryAsync(
            senderMessageId,
            MessageFlag.Ping
        );

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
                sendText += "\n\n<i>Bot run in WebHook mode.</i>" +
                            $"\nUrl WebHook: {getWebHookInfo.Url}" +
                            $"\nUrl Custom Cert: {getWebHookInfo.HasCustomCertificate}" +
                            $"\nAllowed Updates: {getWebHookInfo.AllowedUpdates}" +
                            $"\nPending Count: {(getWebHookInfo.PendingUpdateCount - 1)}" +
                            $"\nMax Connection: {getWebHookInfo.MaxConnections}" +
                            $"\nLast Error: {getWebHookInfo.LastErrorDate:yyyy-MM-dd}" +
                            $"\nError Message: {getWebHookInfo.LastErrorMessage}";
            }
        }

        var sentMessage = await _telegramService.SendTextMessageAsync(
            sendText,
            keyboard
        );

        await _telegramService.SaveToMessageHistoryAsync(
            sentMessage.MessageId,
            MessageFlag.Ping
        );

        op.Complete();
    }
}