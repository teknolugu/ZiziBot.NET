using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.GlobalBan;

public class GlobalBanSyncCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly GlobalBanService _globalBanService;

    public GlobalBanSyncCommand(
        GlobalBanService globalBanService,
        TelegramService telegramService
    )
    {
        _globalBanService = globalBanService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        if (!_telegramService.IsFromSudo) return;

        await _telegramService.SendTextMessageAsync("Sedang sinkronisasi..");

        await _globalBanService.UpdateCache();

        await _telegramService.EditMessageTextAsync($"Sinkronisasi selesai");
    }
}