using System.Linq;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SerilogTimings;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Core
{
    /// <summary>
    /// Handle Start
    /// </summary>
    public class StartHandler : ZiziEventHandler
    {
        private readonly ILogger _logger;
        private readonly BotService _botService;
        private readonly ChatService _chatService;
        private readonly SettingsService _settingsService;
        private readonly EnginesConfig _enginesConfig;

        /// <summary>
        /// Constructor of StartHandler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="envConfig"></param>
        /// <param name="chatService"></param>
        /// <param name="settingsService"></param>
        public StartHandler(
            ILogger<StartHandler> logger,
            IOptionsSnapshot<EnginesConfig> envConfig,
            BotService botService,
            ChatService chatService,
            SettingsService settingsService
        )
        {
            _logger = logger;
            _botService = botService;
            _chatService = chatService;
            _settingsService = settingsService;
            _enginesConfig = envConfig.Value;
        }

        /// <summary>
        /// This method is used to handle /start command
        /// </summary>
        [Command("start", CommandParseMode.Both)]
        public async Task Start()
        {
            var op = Operation.Begin("Command Start Handler on {ChatId}", ChatId);

            var me = await _botService.GetMeAsync();

            var argStr = Message.Text.GetTextWithoutCmd();
            var args = argStr.Split("_");
            var arg1 = args.ElementAt(0);

            switch (arg1)
            {
                case "settings":
                    var chatId = args.ElementAt(1).ToInt64();
                    var settingsCmd = await _settingsService.GetSettingButtonByGroup(chatId, true);

                    var btnMarkup = await settingsCmd.ToJson().JsonToButton(chunk: 2);
                    _logger.LogDebug("Settings: {Count}", settingsCmd.Count);

                    await SendMessageTextAsync("Pengaturan", btnMarkup);
                    break;

                default:
                    var htmlMsg = new HtmlString()
                        .Bold($"🤖 {me.FirstName} ").Code(_enginesConfig.Version).Br()
                        .TextBr($"by @{_enginesConfig.Company}.").Br()
                        .Bold($"{me.FirstName} ")
                        .TextBr(
                            $"adalah bot debugging dan manajemen grup yang di lengkapi dengan alat keamanan. " +
                            "Agar fungsi saya bekerja dengan fitur penuh, jadikan saya admin dengan"
                        )
                        .Url("https://docs.zizibot.winten.my.id/glosarium/admin-dengan-level-standard", "Level standard").Br().Br()
                        .TextBr("Saran dan fitur bisa di ajukan ke grup dibawah ini.")
                        .TextBr("- @WinTenDevSupport")
                        .Text("- @TgBotID");

                    await SendMessageTextAsync(htmlMsg, disableWebPreview: true);
                    break;
            }

            op.Complete();
        }
    }
}