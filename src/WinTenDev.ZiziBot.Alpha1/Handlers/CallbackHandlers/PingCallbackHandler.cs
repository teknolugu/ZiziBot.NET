using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Interfaces;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.CallbackHandlers
{
    /// <summary>
    /// This class is used to handle Ping Callback
    /// </summary>
    public class PingCallbackHandler : ICallbackHandler
    {
        /// <summary>
        /// This method is called when PingCallback handler object created
        /// </summary>
        public PingCallbackHandler()
        {
        }

        /// <summary>
        /// This method is used to handle Callback Sync
        /// </summary>
        /// <param name="callbackQuery"></param>
        /// <param name="onAnswerCallback"></param>
        /// <returns></returns>
        public bool Execute(
            CallbackQuery callbackQuery,
            Func<CallbackAnswer, Task> onAnswerCallback
        )
        {
            var callbackParse = callbackQuery.Data.ParseCallback();

            var answerCallback = "Pong!";

            onAnswerCallback(
                new CallbackAnswer()
                {
                    CallbackAnswerMode = CallbackAnswerMode.AnswerCallback,
                    CallbackAnswerText = answerCallback
                }
            );

            return true;
        }

        /// <summary>
        /// This method is used to handle Callback Async
        /// </summary>
        /// <param name="callbackQuery"></param>
        /// <param name="onAnswerCallback"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> ExecuteAsync(
            CallbackQuery callbackQuery,
            Func<CallbackAnswer, Task> onAnswerCallback
        )
        {
            throw new NotImplementedException();
        }
    }
}