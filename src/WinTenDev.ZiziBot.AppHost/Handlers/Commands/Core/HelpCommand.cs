using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class HelpCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public HelpCommand(TelegramService telegramService)
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

        var sendText = "Untuk mendapatkan bantuan klik tombol dibawah ini";
        var urlStart = await _telegramService.GetUrlStart("start=help");
        var ziziDocs = "https://docs.zizibot.winten.my.id";

        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Dapatkan bantuan", ziziDocs));

        await _telegramService.SendTextMessageAsync(sendText, keyboard);
    }
}