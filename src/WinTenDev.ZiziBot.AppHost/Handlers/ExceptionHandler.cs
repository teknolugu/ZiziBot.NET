using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class ExceptionHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;
    private readonly EventLogService _eventLogService;

    public ExceptionHandler(
        TelegramService telegramService,
        EventLogService eventLogService
    )
    {
        _telegramService = telegramService;
        _eventLogService = eventLogService;
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
        catch (Exception exception)
        {
            var htmlMessage = HtmlMessage.Empty
                .Bold("🗒 Message: ").CodeBr(exception.Message).Br()
                .BoldBr("🔄 Update: ").CodeBr(update.ToJson(true)).Br()
                .BoldBr("🛑 Exception: ").CodeBr(exception.ToStringDemystified()).Br();

            await _eventLogService.SendEventLogCoreAsync(htmlMessage.ToString());
        }
    }
}
