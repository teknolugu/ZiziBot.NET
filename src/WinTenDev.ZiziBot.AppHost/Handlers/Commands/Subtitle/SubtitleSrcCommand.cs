using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Subtitle;

internal class SubtitleSrcCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public SubtitleSrcCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.GetSubtitleSources();
    }
}