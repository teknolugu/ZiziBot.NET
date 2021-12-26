using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

namespace WinTenDev.ZiziBot.App.Handlers.Core
{
    /// <summary>
    /// Handle Power "button"
    /// </summary>
    public class PowerHandler : ZiziEventHandler
    {
        private readonly IHostApplicationLifetime _applicationLifetime;

        /// <summary>
        /// Instantiate PowerHandler
        /// </summary>
        /// <param name="applicationLifetime"></param>
        public PowerHandler(IHostApplicationLifetime applicationLifetime
        )
        {
            _applicationLifetime = applicationLifetime;

            // applicationLifetime.ApplicationStopping.Register(OnShuttingDown().WaitWithoutException);
            // applicationLifetime.ApplicationStopped.Register(OnShutdownFinish().WaitWithoutException);
        }

        /// <summary>
        /// Handle /shutdown command
        /// </summary>
        [Command("shutdown", CommandParseMode.Both)]
        public async Task ShutdownBot()
        {
            var me = await Bot.GetMeAsync();

            var htmlMsg = new HtmlString()
                .Bold($"{me.FirstName}").Text(" dijadwalkan untuk dimatikan.").Br()
                .Text("Jika berjalan dengan Systemd, silakan tunggu beberapa menit sampai dimulai ulang.");

            await SendMessageTextAsync(htmlMsg);

            _applicationLifetime.StopApplication();
        }

        private async Task OnShuttingDown()
        {
            await EditMessageTextAsync(new HtmlString().Text("Sedang mematikan..").Br());
        }

        private async Task OnShutdownFinish()
        {
            // await Task.Delay(500);

            await EditMessageTextAsync(new HtmlString().Text("App berhasil dimatikan..").Br());
        }
    }
}