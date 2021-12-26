using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

internal class StartCommand : CommandBase
{
    private readonly BotService _botService;
    private readonly TelegramService _telegramService;
    private readonly EnginesConfig _enginesConfig;

    public StartCommand(
        IOptionsSnapshot<EnginesConfig> enginesConfig,
        BotService botService,
        TelegramService telegramService
    )
    {
        _botService = botService;
        _telegramService = telegramService;
        _enginesConfig = enginesConfig.Value;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;
        var partText = msg.Text.SplitText(" ").ToArray();
        var paramStart = partText.ValueOfIndex(1);

        var getMe = await _botService.GetMeAsync();
        var urlStart = await _botService.GetUrlStart("start=help");
        var urlAddTo = await _botService.GetUrlStart("startgroup=new");

        var botName = getMe.GetFullName();
        var botVer = _enginesConfig.Version;
        var botCompany = _enginesConfig.Company;

        var winTenDev = botCompany.MkUrl("https://t.me/WinTenDev");
        var levelStandardUrl = "https://docs.zizibot.azhe.space/glosarium/admin-dengan-level-standard";
        var ziziDocs = "https://docs.zizibot.azhe.space";
        var levelStandard = @"Level standard".MkUrl(levelStandardUrl);

        var sendText = $"🤖 {botName} {botVer}" +
                       $"\nby {winTenDev}." +
                       $"\n\nAdalah bot debugging dan manajemen grup yang di lengkapi dengan alat keamanan. " +
                       $"Agar fungsi saya bekerja dengan fitur penuh, jadikan saya admin dengan {levelStandard}. " +
                       $"\n\nSaran dan fitur bisa di ajukan di @WinTenDevSupport atau @TgBotID.";


        switch (paramStart)
        {
            case "set-username":
                var setUsername = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Pasang Username", "https://t.me/WinTenDev/29")
                    }
                });
                var send = "Untuk cara pasang Username, silakan klik tombol di bawah ini";
                await _telegramService.SendTextMessageAsync(send, setUsername);
                break;

            default:
                // var keyboard = new InlineKeyboardMarkup(
                //     InlineKeyboardButton.WithUrl("Dapatkan bantuan", ziziDocs)
                // );
                //
                // if (_telegramService.IsPrivateChat())
                // {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Bantuan", ziziDocs),
                        InlineKeyboardButton.WithUrl("Pasang Username", "https://t.me/WinTenDev/29")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Tambahkan ke Grup", urlAddTo)
                    }
                });
                // }

                await _telegramService.SendTextMessageAsync(sendText, keyboard, disableWebPreview: true);
                break;
        }
    }
}