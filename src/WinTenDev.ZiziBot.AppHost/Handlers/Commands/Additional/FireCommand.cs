using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class FireCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly ChatService _chatService;
    public FireCommand(
        TelegramService telegramService,
        ChatService chatService
    )
    {
        _telegramService = telegramService;
        _chatService = chatService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var messageText = _telegramService.MessageOrEditedText;

        if (_telegramService.ReplyToMessage != null)
        {
            messageText = _telegramService.ReplyToMessage.Text;
        }

        var fireResult = _chatService.FireAnalyzer(messageText);

        await _telegramService.SendTextMessageAsync
        (
            "<b>Fire Result</b>" +
            "\nWord Count: " + fireResult.WordsCount +
            "\nFire Ratio: " + fireResult.FireRatio
        );
    }
}