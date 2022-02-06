using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class ExceptionHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public ExceptionHandler(
        TelegramService telegramService
    )
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        var chatId = _telegramService.ChatId;
        var update = _telegramService.Update;

        try
        {
            await next(context, cancellationToken);
        }
        catch (Exception e)
        {
            Log.Error(e.Demystify(), "Exception handler at ChatId: {ChatId}", chatId);

            var eventBuilder = new StringBuilder()
                .Append("<b>Message: </b>").AppendLine(e.Message)
                .AppendLine()
                .Append("<code>")
                .Append(update.ToJson(true))
                .Append("</code>");

            await _telegramService.SendEventLogRawAsync(eventBuilder.ToTrimmedString());
        }
    }
}