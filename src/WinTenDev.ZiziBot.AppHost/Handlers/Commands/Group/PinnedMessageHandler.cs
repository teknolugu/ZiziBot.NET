using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;

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

        var msg = _telegramService.Message;

        var pinnedMsg = msg.PinnedMessage;
        var sendText = $"📌 Pesan di sematkan baru!" +
                       $"\nPengirim: {pinnedMsg.GetFromNameLink()}" +
                       $"\nPengepin: {msg.GetFromNameLink()}";

        await _telegramService.SendTextMessageAsync(sendText);
    }
}