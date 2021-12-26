using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Enums;
using BotFramework.Setup;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using Telegram.Bot;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.ZiziBot.App.Handlers.CallbackHandlers;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers
{
    /// <summary>
    /// This class is used to handle Callback Query
    /// </summary>
    public class CallbackHandler : ZiziEventHandler
    {
        private readonly ILogger<CallbackHandler> _logger;
        private readonly SettingsCallbackHandler _settingsCallbackHandler;
        private readonly PingCallbackHandler _pingCallbackHandler;

        /// <summary>
        /// Constructor of CallbackHandler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settingsCallbackHandler"></param>
        /// <param name="pingCallbackHandler"></param>
        public CallbackHandler(
            ILogger<CallbackHandler> logger,
            SettingsCallbackHandler settingsCallbackHandler,
            PingCallbackHandler pingCallbackHandler
        )
        {
            _logger = logger;
            _settingsCallbackHandler = settingsCallbackHandler;
            _pingCallbackHandler = pingCallbackHandler;
        }

        /// <summary>
        /// This function is used to handle on Callback Query
        /// </summary>
        [Update(InChat.All, UpdateFlag.CallbackQuery)]
        public async Task OnCallback()
        {
            var op = Operation.Begin("Callback Update");

            var callbackQueryData = CallbackQuery.Data;
            var callbackParse = callbackQueryData.ParseCallback();
            var callbackCmd = callbackParse.CallbackDataCmd;

            _logger.LogInformation("CallbackMessage on ChatId '{ChatId}' => '{CallbackText}'",
            ChatId, callbackQueryData);

            var callbackResult = callbackCmd switch
            {
                "ping" => _pingCallbackHandler.Execute(CallbackQuery, AnswerCallbackAsync),
                "setting" => await _settingsCallbackHandler.ExecuteAsync(CallbackQuery, answer => AnswerCallbackAsync(answer)),
                _ => false
            };

            _logger.LogInformation("Callback Result on ChatId '{ChatId}' => '{CallbackResult}'",
            ChatId, callbackResult);

            op.Complete();
        }

        /// <summary>
        /// This function is used to answer Callback Query
        /// </summary>
        /// <param name="callbackAnswer"></param>
        private async Task AnswerCallbackAsync(CallbackAnswer callbackAnswer)
        {
            var callbackQueryId = CallbackQuery.Id;
            var answerMode = callbackAnswer.CallbackAnswerMode;
            var answerCallback = callbackAnswer.CallbackAnswerText;

            switch (answerMode)
            {
                case CallbackAnswerMode.AnswerCallback:
                    await Bot.AnswerCallbackQueryAsync(callbackQueryId, answerCallback, showAlert: true);
                    break;
                case CallbackAnswerMode.SendMessage:
                    break;
                case CallbackAnswerMode.EditMessage:
                    var messageText = callbackAnswer.CallbackAnswerText;
                    var messageMarkup = callbackAnswer.CallbackAnswerInlineMarkup;
                    await EditMessageTextAsync(messageText, messageMarkup);
                    break;
                default:
                    _logger.LogDebug("No Callback Answer mode for CallBack {CallbackQueryId}. {V}", callbackQueryId, answerMode);
                    break;
            }
        }
    }
}