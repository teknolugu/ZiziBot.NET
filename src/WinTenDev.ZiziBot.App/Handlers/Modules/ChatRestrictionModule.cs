using System;
using System.Linq;
using System.Threading.Tasks;
using BotFramework.Utils;
using Microsoft.Extensions.Options;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.ZiziBot.App.Handlers.Modules
{
    /// <summary>
    /// This is module for Chat Restriction
    /// </summary>
    public class ChatRestrictionModule
    {
        private readonly RestrictionConfig _restrictionConfig;

        /// <summary>
        /// Instantiate class
        /// </summary>
        /// <param name="restrictionConfigOpt"></param>
        public ChatRestrictionModule
        (
            IOptionsSnapshot<RestrictionConfig> restrictionConfigOpt
        )
        {
            _restrictionConfig = restrictionConfigOpt.Value;
        }

        /// <summary>
        /// Run Chat Restriction by chatId
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="funcChatRestricted"></param>
        /// <returns></returns>
        public async Task<bool> CheckRestriction(long chatId, Func<ChatRestrictionResult, Task> funcChatRestricted)
        {
            Log.Information("Checking Chat Restriction for {ChatId}", chatId);

            var chatRestrictionResult = new ChatRestrictionResult
            {
                IsEnabled = _restrictionConfig.EnableRestriction
            };

            if (!_restrictionConfig.EnableRestriction)
            {
                Log.Debug("Chat Restriction is not enabled in this chat!");
                return false;
            }

            chatRestrictionResult.ChatId = chatId;

            var checkRestricted = !_restrictionConfig.RestrictionArea.Contains(chatId.ToString());
            Log.Debug("Is ChatId {ChatId} restricted? {Check}", chatId, checkRestricted);

            if (checkRestricted)
            {
                chatRestrictionResult.DoLeaveChat = true;
                chatRestrictionResult.IsRestricted = true;

                chatRestrictionResult.HtmlMessage = new HtmlString()
                    .Text("Tampaknya saya dibatasi, mungkin karena masih Beta. ")
                    .Text("Silakan gunakan Zizi Bot untuk lingkungan yang lebih stabil.").Br();
            }

            await funcChatRestricted(chatRestrictionResult);

            return chatRestrictionResult.IsRestricted;
        }
    }
}