using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class AboutCommand : CommandBase
{
    private readonly BotService _botService;
    private readonly TelegramService _telegramService;
    private readonly EnginesConfig _enginesConfig;

    public AboutCommand(
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

        var me = await _botService.GetMeAsync();
        var botVersion = _enginesConfig.Version;
        var company = _enginesConfig.Company;

        var sendText = $"<b>{company} {me.GetFullName()}</b>" +
                       $"\nby @WinTenDev" +
                       $"\nVersion: {botVersion}" +
                       "\n\nℹ️ Bot Telegram resmi berbasis <b>WinTen API.</b> untuk manajemen dan peralatan grup. " +
                       "Ditulis menggunakan .NET (C#). " +
                       "Untuk detail fitur pada perintah /start.\n";

        if (await _botService.IsBeta())
        {
            sendText += "\n<b>Saya masih Beta, mungkin terdapat bug dan tidak stabil. " +
                        "Tidak dapat di tambahkan ke grup Anda.</b>\n";
        }

        sendText += "\nUntuk Bot lebih cepat dan tetap cepat dan terus peningkatan dan keandalan, " +
                    "silakan <b>Donasi</b> untuk biaya Server dan beri saya Kopi.\n\n" +
                    "Terima kasih kepada <b>Akmal Projext</b> yang telah memberikan kesempatan ZiziBot pada kehidupan sebelumnya.";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("👥 WinTen Group", "https://t.me/WinTenGroup"),
                InlineKeyboardButton.WithUrl("❤️ WinTen Dev", "https://t.me/WinTenDev")
            },
            new[]
            {
                InlineKeyboardButton.WithUrl("👥 Redmi 5A (ID)", "https://t.me/Redmi5AID"),
                InlineKeyboardButton.WithUrl("👥 Telegram Bot API", "https://t.me/TgBotID")
            },
            new[]
            {
                InlineKeyboardButton.WithUrl("💽 Source Code (.NET)", "https://github.com/WinTenDev/WinTenBot.NET"),
                InlineKeyboardButton.WithUrl("🏗 Akmal Projext", "https://t.me/AkmalProjext")
            },
            new[]
            {
                InlineKeyboardButton.WithUrl("💰 Donate", "https://paypal.me/Azhe403"),
                InlineKeyboardButton.WithUrl("💰 Dana.ID", "https://link.dana.id/qr/5xcp0ma"),
                InlineKeyboardButton.WithUrl("💰 Saweria", "https://saweria.co/azhe403")
            }
        });

        await _telegramService.SendTextMessageAsync(sendText, inlineKeyboard, 0);
    }
}