using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

internal class ChatJoinRequestHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public ChatJoinRequestHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AnswerChatJoinRequestAsync();
    }
}