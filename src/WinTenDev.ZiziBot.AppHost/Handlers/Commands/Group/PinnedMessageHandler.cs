using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class PinnedMessageHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public PinnedMessageHandler(TelegramService telegramService)
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

        var message = _telegramService.Message;

        var pinnedMsg = message.PinnedMessage;

        var messageLink = pinnedMsg.GetMessageLink();
        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("➡ Ke Pesan", messageLink));

        var sendText = $"📌 Pesan di sematkan baru!" +
                       $"\nPengirim: {pinnedMsg.GetFromNameLink()}" +
                       $"\nPengepin: {message.GetFromNameLink()}";

        await _telegramService.SendTextMessageAsync(sendText, keyboard, replyToMsgId: 0);
    }
}
