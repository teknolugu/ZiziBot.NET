using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Serilog;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Interfaces;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.CallbackHandlers
{
    /// <summary>
    /// This class is used to handle Settings Callback
    /// </summary>
    public class SettingsCallbackHandler : ICallbackHandler
    {
        private readonly SettingsService _settingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsCallbackHandler" /> class.
        /// </summary>
        /// <param name="settingsService">The settings service.</param>
        public SettingsCallbackHandler(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// This is function is used to execute Callback Sync
        /// </summary>
        /// <param name="callbackQuery"></param>
        /// <param name="onAnswerCallback"></param>
        /// <returns>boolean</returns>
        public bool Execute(
            CallbackQuery callbackQuery,
            Func<CallbackAnswer, Task> onAnswerCallback
        )
        {
            return true;
        }

        /// <summary>
        /// This is function is used to execute Callback Async
        /// </summary>
        /// <param name="callbackQuery">The callback query.</param>
        /// <param name="onAnswerCallback">The on answer callback.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public async Task<bool> ExecuteAsync(
            CallbackQuery callbackQuery,
            Func<CallbackAnswer, Task> onAnswerCallback
        )
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var callbackParse = callbackQuery.Data.ParseCallback();
            var callbackDataSplit = callbackParse.CallbackDataSplit;

            var callbackArgs = callbackParse.CallbackArgs;
            var callbackArgStr = callbackParse.CallbackArgStr;

            var currentValueStr = callbackArgs.ElementAt(0);
            var currentKeyStr = callbackArgStr.Replace(currentValueStr, "");
            var currentKeyName = currentKeyStr.Humanize().Titleize();
            var currentValueInt = currentValueStr.ToBoolInt();

            var chatIdx = callbackDataSplit.ElementAtOrDefault(2);
            var appendChatId = chatIdx != null;

            if (chatIdx != null) chatId = chatIdx.ToInt64();

            var columnName = "enable" + currentKeyStr;
            var updatedValue = currentValueInt == 0 ? 1 : 0;

            var data = new Dictionary<string, object>()
            {
                ["chat_id"] = chatId,
                [columnName] = updatedValue
            };

            var header = $"ChatID: {chatId}\n";

            await onAnswerCallback(
                new CallbackAnswer()
                {
                    CallbackAnswerMode = CallbackAnswerMode.EditMessage,
                    CallbackAnswerText = $"{header}\nSedang memberbarui {currentKeyName}.."
                }
            );

            await _settingsService.SaveSettingsAsync(data);

            var settingBtn = await _settingsService.GetSettingButtonByGroup(chatId, appendChatId);
            var btnMarkup = await settingBtn.ToJson().JsonToButton(chunk: 2);
            Log.Debug("Settings: {Count}", settingBtn.Count);

            await onAnswerCallback(
                new CallbackAnswer()
                {
                    CallbackAnswerMode = CallbackAnswerMode.EditMessage,
                    CallbackAnswerText = $"{header}\n{currentKeyName} berhasil di berbarui.",
                    CallbackAnswerInlineMarkup = btnMarkup
                }
            );

            await _settingsService.UpdateCacheAsync(chatId);

            return true;
        }
    }
}