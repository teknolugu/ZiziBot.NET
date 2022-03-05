using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;

public class SetWelcomeCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public SetWelcomeCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        if (_telegramService.IsPrivateChat)
        {
            await _telegramService.SendTextMessageAsync("Atur pesan Welcome hanya untuk grup saja");
            return;
        }

        if (!await _telegramService.CheckFromAdminOrAnonymous()) return;

        await _telegramService.SaveWelcomeSettingsAsync();
    }
}