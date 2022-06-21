using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using Microsoft.Extensions.Logging;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.ZiziBot.Alpha1.Handlers.Core;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Group
{
    /// <summary>
    /// Handle about Welcome message configuration
    /// </summary>
    public class WelcomeHandler : ZiziEventHandler
    {
        private readonly ILogger<WelcomeHandler> _logger;
        private readonly SettingsService _settingsService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settingsService"></param>
        public WelcomeHandler(
            ILogger<WelcomeHandler> logger,
            SettingsService settingsService
        )
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        [Command("welmsg", CommandParseMode.Both)]
        public async Task SetWelcomeMessage()
        {
            if (IsPrivateChat()) return;

            var columnTarget = $"welcome_message";

            await SaveWelcomeAsync(columnTarget);
        }

        [Command("welbtn", CommandParseMode.Both)]
        public void SetWelcomeButton()
        {
            if (IsPrivateChat()) return;

        }

        [Command("weldoc", CommandParseMode.Both)]
        public void SetWelcomeDocument()
        {
            if (IsPrivateChat()) return;

        }

        [Command("resetwelcome", CommandParseMode.Both)]
        public void ResetWelcomeDocument()
        {
            if (IsPrivateChat()) return;

        }

        private async Task SaveWelcomeAsync(string columnTarget)
        {
            var data = Message.Text.GetTextWithoutCmd();

            if (Message.ReplyToMessage != null)
            {
                data = Message.ReplyToMessage.Text;
            }

            if (data.IsNullOrEmpty())
            {
                await SendMessageTextAsync($"Silakan masukan konfigurasi Pesan yang akan di terapkan");
                return;
            }

            await SendMessageTextAsync($"Sedang menyimpan Welcome Message..");

            await _settingsService.UpdateCell(
                Chat.Id,
                columnTarget,
                data
            );

            await EditMessageTextAsync(
                $"Welcome Button berhasil di simpan!" +
                $"\nKetik /welcome untuk melihat perubahan"
            );
        }
    }
}