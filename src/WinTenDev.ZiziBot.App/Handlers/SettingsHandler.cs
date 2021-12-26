using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers
{
    /// <summary>
    /// This class is used to handle about Settings command
    /// </summary>
    public class SettingsHandler : ZiziEventHandler
    {
        private readonly ChatService _chatService;
        private readonly ILogger<SettingsHandler> _logger;
        private readonly BotService _botService;
        private readonly SettingsService _settingsService;

        /// <summary>
        /// Constructor of SettingsHandler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="chatService"></param>
        /// <param name="settingsService"></param>
        public SettingsHandler(
            ILogger<SettingsHandler> logger,
            BotService botService,
            ChatService chatService,
            SettingsService settingsService
        )
        {
            _logger = logger;
            _botService = botService;
            _chatService = chatService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// This method is used to handle about Settings command to Private
        /// </summary>
        [Command("settings", CommandParseMode.Both)]
        public async Task CmdGetSettingsInPrivate()
        {
            var payload = $"start=settings_{ChatId}";

            var startSettings = await _botService.GetUrlStart(payload);

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("Pengaturan", startSettings)
                }
            });

            await SendMessageTextAsync("Tekan tombol di bawha ini untuk mengubah pengaturan", replyMarkup: inlineKeyboard);
        }

        /// <summary>
        /// This method is used to handle /settings command to current
        /// </summary>
        [Command("insettings", CommandParseMode.Both)]
        public async Task CmdGetSettingsInCurrent()
        {
            var settingsCmd = await _settingsService.GetSettingButtonByGroup(ChatId);

            var btnMarkup = await settingsCmd.ToJson().JsonToButton(chunk: 2);
            _logger.LogDebug("Settings: {Count}", settingsCmd.Count);

            await SendMessageTextAsync("Pengaturan", btnMarkup);
        }
    }
}