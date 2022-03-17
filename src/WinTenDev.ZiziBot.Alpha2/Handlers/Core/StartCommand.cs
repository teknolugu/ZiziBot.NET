using System.Threading;
using System.Threading.Tasks;
using TgBotFramework;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.Alpha2.Handlers.Core;

public class StartCommand : CommandBase<UpdateContext>
{
    private readonly TelegramService _telegramService;

    public StartCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;

    }

    public override async Task HandleAsync(
        UpdateContext context,
        UpdateDelegate<UpdateContext> next,
        string[] args,
        CancellationToken cancellationToken
    )
    {
        var chatId = context.ChatId;

        await _telegramService.SendTextMessageAsync("Hi there!");
    }
}