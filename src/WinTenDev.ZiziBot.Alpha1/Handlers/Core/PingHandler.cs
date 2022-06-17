using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Core
{
    /// <summary>
    /// Handler for Ping
    /// </summary>
    public class PingHandler : ZiziEventHandler
    {
        private readonly ILogger<PingHandler> _logger;
        private readonly PrivilegeService _privilegeService;

        /// <summary>
        /// Constructor of <see cref="PingHandler" /> instance
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="privilegeService"></param>
        public PingHandler(
            ILogger<PingHandler> logger,
            PrivilegeService privilegeService
        )
        {
            _logger = logger;
            _privilegeService = privilegeService;
        }

        /// <summary>
        /// Handle ping command
        /// Usage: <code>/ping</code>
        /// </summary>
        [Command("ping", CommandParseMode.Both)]
        public async Task Ping()
        {
            using var operation = Operation.Begin("Ping command handler");

            var pingOffset = TimeSpan.FromSeconds(60);
            if (IsMessageOlderThan(pingOffset))
            {
                _logger.LogDebug("Ping response is disabled because chatId is older than {PingOffset}", pingOffset);
                return;
            }

            var isSudo = _privilegeService.IsFromSudo(From.Id);

            var currProcess = Process.GetCurrentProcess();
            var processStartTime = currProcess.StartTime;
            var processUptime = (DateTime.Now - processStartTime);

            var htmlMsg = new HtmlString()
                .Bold("🏓 Pong!");

            if (isSudo)
            {
                htmlMsg.Br().Br()
                    .Bold("🏃 Runtime: ").Code(processStartTime.ToString("yyyy-MM-dd HH:mm")).Br()
                    .Bold("⏱ Uptime: ").Code(processUptime.ToHumanDuration());
            }

            var keyboardMarkup = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Ping", "ping")
                    }
                }
            );

            await SendMessageTextAsync(
                message: htmlMsg,
                replyMarkup: keyboardMarkup,
                replyToMessageId: Message.MessageId
            );

            operation.Complete();
        }
    }
}