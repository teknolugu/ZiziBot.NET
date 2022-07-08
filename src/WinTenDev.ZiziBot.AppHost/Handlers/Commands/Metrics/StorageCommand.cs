using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Metrics;

public class StorageCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public StorageCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        if (!_telegramService.IsFromSudo)
        {
            return;
        }

        await _telegramService.SendTextMessageAsync("<b>Storage Sense</b>");

        // var cachePath = BotSettings.PathCache.DirSize();
        // await _telegramService.AppendTextAsync($"<b>Cache Size: </b> {cachePath}");
    }
}
