using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class AdminCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public AdminCommand(TelegramService telegramService)
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
            await _telegramService.SendTextMessageAsync("Tidak ada Admin di obrolan Private");
            return;
        }

        await _telegramService.SendTextMessageAsync("🔄 Sedang mengambil data..");

        var sendText = await _telegramService.GetChatAdminList();

        await _telegramService.EditMessageTextAsync(sendText);
    }
}