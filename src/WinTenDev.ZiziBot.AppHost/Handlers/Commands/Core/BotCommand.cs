using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class BotCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public BotCommand(
        TelegramService telegramService,
        IHostApplicationLifetime applicationLifetime
    )
    {
        _telegramService = telegramService;
        _applicationLifetime = applicationLifetime;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var isSudoer = _telegramService.IsFromSudo;
        if (!isSudoer) return;

        var param1 = _telegramService.MessageTextParts.ValueOfIndex(1);
        switch (param1)
        {
            case "shutdown":
                await _telegramService.SendTextMessageAsync("Bot di jadwalkan untuk dimatikan..");
                _applicationLifetime.StopApplication();

                break;
        }
    }
}