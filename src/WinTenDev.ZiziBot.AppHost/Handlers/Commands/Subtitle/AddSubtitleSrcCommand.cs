using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Subtitle;

internal class AddSubtitleSrcCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public AddSubtitleSrcCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddSubtitleSource();
    }
}