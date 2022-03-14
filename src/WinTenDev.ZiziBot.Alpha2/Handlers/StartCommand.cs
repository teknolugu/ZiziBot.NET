using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TgBotFramework;

namespace WinTenDev.ZiziBot.Alpha2.Handlers;

public class StartCommand:CommandBase<UpdateContext>
{

    public override async Task HandleAsync(
        UpdateContext context,
        UpdateDelegate<UpdateContext> next,
        string[] args,
        CancellationToken cancellationToken
    )
    {
        var chatId = context.ChatId;

        await context.Client.SendTextMessageAsync(chatId,"Hi there!");
    }
}