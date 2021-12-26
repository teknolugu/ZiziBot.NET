using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using SerilogTimings;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.Additional
{
    /// <summary>
    /// Handle about string utilities
    /// </summary>
    public class StringHandler : ZiziEventHandler
    {
        /// <summary>
        /// Get length of text from given string/message text
        /// </summary>
        [Command("len", CommandParseMode.Both)]
        public async Task Len()
        {
            var message = RawUpdate.Message;
            var messageText = message.Text.GetTextWithoutCmd();

            if (ReplyToMessage != null)
            {
                message = message.ReplyToMessage;
                messageText = message.Text;
            }

            if (messageText == null)
            {
                await SendMessageTextAsync("Sepertinya ini bukan pesan teks", replyToMessageId: message.MessageId);
                return;
            }

            var length = messageText.Length;
            var htmlMessage = new HtmlString()
                .Text("Jumlah Karakter: " + length);

            await SendMessageTextAsync(htmlMessage, replyToMessageId: message.MessageId);
        }

        /// <summary>
        /// Get length of text from given string/message text
        /// </summary>
        [Command("an", CommandParseMode.Both)]
        public async Task Analyze()
        {
            var message = RawUpdate.Message;
            var messageText = message.Text.GetTextWithoutCmd();

            if (ReplyToMessage != null)
            {
                message = message.ReplyToMessage;
                messageText = message.Text;
            }

            if (messageText == null)
            {
                await SendMessageTextAsync("Sepertinya ini bukan pesan teks", replyToMessageId: message.MessageId);
                return;
            }

            var result = messageText.AnalyzeString();
            var htmlMessage = new HtmlString()
                .Text("Analyze result").Br()
                .Text("Fire ratio: " + result.FireRatio);

            await SendMessageTextAsync(htmlMessage, replyToMessageId: message.MessageId);
        }

        /// <summary>
        /// This method is used to handle /tr command
        /// </summary>
        [Command("tr", CommandParseMode.Both)]
        public async Task CmdTranslate()
        {
            var op = Operation.Begin("Command Translate (/tr)");

            var messageText = Message.Text.GetTextWithoutCmd();
            if (ReplyToMessage != null) messageText = ReplyToMessage.Text;

            if (messageText.IsNullOrEmpty())
            {
                await SendMessageTextAsync("Silakan reply pesan untuk menerjemahkan");

                op.Complete();
                return;
            }

            var targetLang = From.LanguageCode;

            if (targetLang == null) targetLang = "en";

            var translate = await messageText.GoogleTranslatorAsync(targetLang);

            await SendMessageTextAsync(translate);

            op.Complete();
        }
    }
}