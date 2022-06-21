using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Core
{
    /// <summary>
    /// Handle About
    /// </summary>
    public class AboutHandler : ZiziEventHandler
    {
        private readonly EnginesConfig _enginesConfig;

        /// <summary>
        /// Constructor of AboutHandler
        /// </summary>
        /// <param name="enginesConfig"></param>
        public AboutHandler(IOptionsSnapshot<EnginesConfig> enginesConfig)
        {
            _enginesConfig = enginesConfig.Value;
        }

        /// <summary>
        /// Handle when user send <code>/about</code> into Chat
        /// </summary>
        [Command("about", CommandParseMode.Both)]
        public async Task About()
        {
            var me = await Bot.GetMeAsync();

            var htmlMsg = new HtmlString()
                .Bold($"🤖 {me.FirstName} ").Code(_enginesConfig.Version).Br()
                .TextBr($"by @{_enginesConfig.Company}.").Br()
                .TextBr(
                    $"ℹ️ Bot Telegram resmi berbasis WinTen API. untuk manajemen dan peralatan grup. " +
                    $"Ditulis menggunakan .NET (C#). Untuk detail fitur pada perintah /start."
                ).Br()
                .TextBr(
                    "Untuk Bot lebih cepat dan tetap cepat dan terus peningkatan dan keandalan, " +
                    "silakan Donasi untuk biaya Server dan beri saya Kopi."
                ).Br()
                .Text("Terima kasih kepada ").Bold("Akmal Projext")
                .Text(" yang telah memberikan kesempatan ").Bold("ZiziBot").Text(" pada kehidupan sebelumnya.");

            var keyboardMarkup = new InlineKeyboardMarkup(
                new[]
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
                        InlineKeyboardButton.WithUrl("💽 Source Code", "https://github.com/WinTenDev/WinTenBot.NET"),
                        InlineKeyboardButton.WithUrl("🏗 Akmal Projext", "https://t.me/AkmalProjext")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("💰 Dana.ID", "https://link.dana.id/qr/5xcp0ma"),
                        InlineKeyboardButton.WithUrl("💰 Saweria", "https://saweria.co/azhe403")
                    }
                }
            );

            await SendMessageTextAsync(htmlMsg, keyboardMarkup);
        }
    }
}