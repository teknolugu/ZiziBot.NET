using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class OutCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public OutCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var partsMsg = _telegramService.MessageTextParts;

        await _telegramService.DeleteSenderMessageAsync();

        if (!_telegramService.IsFromSudo) return;

        var sendText = "Maaf, saya harus keluar";

        if (partsMsg.ValueOfIndex(2) != null)
        {
            sendText += $"\n{partsMsg.ValueOfIndex(1)}";
        }

        var chatId = partsMsg.ValueOfIndex(1).ToInt64();
        Log.Information("Target out: {ChatId}", chatId);

        await _telegramService.SendTextMessageAsync(sendText, customChatId: chatId, replyToMsgId: 0);
        await _telegramService.LeaveChat(chatId);
    }
}