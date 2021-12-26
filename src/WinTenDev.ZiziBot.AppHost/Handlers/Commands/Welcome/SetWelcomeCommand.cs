using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;

public class SetWelcomeCommand : CommandBase
{
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;

    public SetWelcomeCommand(TelegramService telegramService, SettingsService settingsService)
    {
        _telegramService = telegramService;
        _settingsService = settingsService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = context.Update.Message;

        if (msg.Chat.Type == ChatType.Private)
        {
            await _telegramService.SendTextMessageAsync("Welcome hanya untuk grup saja");
            return;
        }

        if (!await _telegramService.CheckFromAdmin()) return;

        await _telegramService.SendTextMessageAsync("/setwelcome sudah di pisah menjadi /welmsg, /welbtn dan /weldoc");
    }
}