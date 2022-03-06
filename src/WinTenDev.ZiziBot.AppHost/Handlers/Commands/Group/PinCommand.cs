using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class PinCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public PinCommand(TelegramService telegramService)
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

        var client = _telegramService.Client;
        var message = _telegramService.MessageOrEdited;
        var chatId = _telegramService.ChatId;

        const string sendText = "Balas pesan yang akan di pin";

        await _telegramService.DeleteSenderMessageAsync();

        if (!await _telegramService.CheckFromAdminOrAnonymous())
        {
            Log.Warning("Pin message only for Admin on Current Chat");
            return;
        }

        if (message.ReplyToMessage != null)
        {
            var messageId = message.ReplyToMessage.MessageId;

            await client.UnpinChatMessageAsync(chatId, messageId);
            await client.PinChatMessageAsync(chatId, messageId);

            return;
        }

        await _telegramService.SendTextMessageAsync(sendText, replyToMsgId: message.MessageId);
    }
}