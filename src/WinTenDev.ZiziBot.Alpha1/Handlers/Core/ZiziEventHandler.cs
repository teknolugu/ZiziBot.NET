using System;
using System.IO;
using System.Threading.Tasks;
using BotFramework;
using BotFramework.Utils;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;
using WinTenDev.Zizi.Utils.Telegram;
using SystemFile=System.IO.File;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Core
{
    /// <summary>
    /// Zizi Handler base
    /// </summary>
    public abstract class ZiziEventHandler : BotEventHandler
    {
        protected long FromId => From.Id;
        protected long ChatId => (Chat ?? CallbackQueryMessage.Chat).Id;

        protected CallbackQuery CallbackQuery => RawUpdate.CallbackQuery;
        protected Message CallbackQueryMessage => CallbackQuery?.Message;

        protected Message Message => RawUpdate.Message;
        protected Message EditedMessage => RawUpdate.EditedMessage;
        protected Message MessageOrEdited => EditedMessage ?? Message;
        protected Message ReplyToMessage => Message.ReplyToMessage;
        protected Message ReplyToMessageOrEdited => MessageOrEdited.ReplyToMessage;

        private string TimeInit => RawUpdate.Message == null ? string.Empty : RawUpdate.Message.Date.GetDelay();

        private string TimeProc { get; set; }

        protected bool HasUsername => From.Username != null;

        protected bool NoUsername => From.Username == null;

        protected Message SentMessage { get; private set; }

        protected Message SentEditedMessage { get; private set; }

        protected bool IsPublicChat()
        {
            var chat = Chat;
            if (CallbackQuery != null)
            {
                chat = CallbackQueryMessage.Chat;
            }

            var isPublicChat = chat.Type is ChatType.Group or ChatType.Supergroup;
            return isPublicChat;
        }

        /// <summary>
        /// This method is used to check chat type, if Private will be return true, otherwise return false
        /// </summary>
        /// <returns></returns>
        protected bool IsPrivateChat()
        {
            var chat = Chat;
            if (CallbackQuery != null)
            {
                chat = CallbackQueryMessage.Chat;
            }

            var isPrivateChat = chat.Type == ChatType.Private;
            return isPrivateChat;
        }

        /// <summary>
        /// Send message with string message parameter
        /// </summary>
        /// <param name="messageStr"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="parseMode"></param>
        /// <param name="disableWebPreview"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="chatId"></param>
        /// <param name="addStamp"></param>
        /// <returns></returns>
        protected async Task<Message> SendMessageTextAsync(
            string messageStr,
            IReplyMarkup replyMarkup = null,
            ParseMode parseMode = ParseMode.Html,
            bool disableWebPreview = false,
            int replyToMessageId = -1,
            ChatId chatId = null,
            bool addStamp = true
        )
        {
            var message = new HtmlString().Text(messageStr);
            return await SendMessageTextAsync(
                message: message,
                replyMarkup: replyMarkup,
                parseMode: parseMode,
                disableWebPreview: disableWebPreview,
                chatId: chatId,
                replyToMessageId: replyToMessageId,
                addStamp: addStamp
            );
        }

        /// <summary>
        /// Send message Text
        /// </summary>
        /// <param name="message"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="parseMode"></param>
        /// <param name="disableWebPreview"></param>
        /// <param name="chatId"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="addStamp"></param>
        protected async Task<Message> SendMessageTextAsync(
            HtmlString message,
            IReplyMarkup replyMarkup = null,
            ParseMode parseMode = ParseMode.Html,
            bool disableWebPreview = false,
            ChatId chatId = null,
            int replyToMessageId = -1,
            bool addStamp = true
        )
        {
            try
            {
                if (message.ToString().Trim().IsNullOrEmpty())
                {
                    Log.Debug("Send Message Text to '{ChatId}' skipped because null or empty", ChatId);
                    return null;
                }

                if (chatId == null) chatId = Chat.Id;

                if (!message.ToString().EndsWith(Environment.NewLine + Environment.NewLine)) message.Br();

                TimeProc = RawUpdate.Message == null ? string.Empty : RawUpdate.Message.Date.GetDelay();

                if (addStamp)
                {
                    message = new HtmlString()
                        .Append(message).Br()
                        .Code($"⏱ {TimeInit} s").Text(" | ").Code($"⌛ {TimeProc} s");
                }

                Log.Debug("Sending message to {Chat}", chatId);

                var messageStr = message.ToString();

                SentMessage = await Bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: messageStr,
                    parseMode: parseMode,
                    disableWebPagePreview: disableWebPreview,
                    replyMarkup: replyMarkup,
                    replyToMessageId: replyToMessageId
                );
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "Message: {V}",
                    e.Message
                );
            }

            return SentMessage;
        }

        /// <summary>
        /// Edit message with string message parameter
        /// </summary>
        /// <param name="messageStr"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="parseMode"></param>
        /// <param name="targetMessageId"></param>
        /// <param name="chatId"></param>
        /// <param name="addStamp"></param>
        /// <returns></returns>
        protected async Task<Message> EditMessageTextAsync(
            string messageStr,
            InlineKeyboardMarkup replyMarkup = null,
            ParseMode parseMode = ParseMode.Html,
            int targetMessageId = 0,
            ChatId chatId = null,
            bool addStamp = true
        )
        {
            var message = new HtmlString().Text(messageStr);
            return await EditMessageTextAsync(
                message,
                replyMarkup,
                parseMode,
                targetMessageId,
                chatId,
                addStamp
            );
        }

        /// <summary>
        /// Edit message Text
        /// </summary>
        /// <param name="message"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="parseMode"></param>
        /// <param name="targetMessageId"></param>
        /// <param name="chatId"></param>
        /// <param name="addStamp"></param>
        /// <returns></returns>
        protected async Task<Message> EditMessageTextAsync(
            HtmlString message,
            InlineKeyboardMarkup replyMarkup = null,
            ParseMode parseMode = ParseMode.Html,
            int targetMessageId = 0,
            ChatId chatId = null,
            bool addStamp = true
        )
        {
            if (chatId == null) chatId = ChatId;
            if (targetMessageId == 0 &&
                SentMessage != null) targetMessageId = SentMessage.MessageId;
            if (CallbackQuery != null) targetMessageId = CallbackQueryMessage.MessageId;

            if (!message.ToString().EndsWith(Environment.NewLine + Environment.NewLine)) message.Br();

            TimeProc = RawUpdate.Message == null ? string.Empty : RawUpdate.Message.Date.GetDelay();

            if (addStamp)
            {
                message = new HtmlString()
                    .Append(message).Br()
                    .Code($"⏱ {TimeInit} s").Text(" | ").Code($"⌛ {TimeProc} s");
            }

            Log.Debug(
                "Editing message in '{Chat} for MessageId '{TargetMessageId}'",
                chatId,
                targetMessageId
            );
            SentEditedMessage = await Bot.EditMessageTextAsync(
                chatId: chatId,
                replyMarkup: replyMarkup,
                messageId: targetMessageId,
                text: message.ToString(),
                parseMode: parseMode
            );

            return EditedMessage;
        }

        /// <summary>
        /// This method is used to promote or demote given userId on message target
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="promote"></param>
        /// <returns></returns>
        protected async Task<TelegramResult> PromoteMember(
            long userId,
            bool promote
        )
        {
            var result = new TelegramResult();

            try
            {
                await Bot.PromoteChatMemberAsync(
                    chatId: ChatId,
                    userId: userId,
                    isAnonymous: promote,
                    canManageChat: promote,
                    canChangeInfo: promote,
                    canPostMessages: promote,
                    canEditMessages: promote,
                    canDeleteMessages: promote,
                    canManageVideoChats: promote
                );

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.IsSuccess = false;
            }

            return result;
        }

        /// <summary>
        /// Send Media or Document
        /// </summary>
        /// <param name="fileIdOrPath"></param>
        /// <param name="mediaType"></param>
        /// <param name="caption"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="replyToMsgId"></param>
        protected async Task SendMediaDocumentAsync(
            string fileIdOrPath,
            MediaType mediaType,
            HtmlString caption = null,
            IReplyMarkup replyMarkup = null,
            int replyToMsgId = 0
        )
        {
            if (caption != null)
            {
                if (!caption.ToString().EndsWith(Environment.NewLine + Environment.NewLine)) caption.Br();

                caption = new HtmlString()
                    .Append(caption).Br()
                    .Code($"⏱ {TimeInit} s").Text(" | ").Code($"⌛ {TimeProc} s");
            }

            switch (mediaType)
            {
                case MediaType.LocalDocument:
                    var fileName = Path.GetFileName(fileIdOrPath);
                    await using (var fs = SystemFile.OpenRead(fileIdOrPath))
                    {
                        var inputOnlineFile = new InputOnlineFile(fs, fileName);
                        // var inputThumb = new InputMedia(fs, fileName);

                        SentMessage = await Bot.SendDocumentAsync(
                            Chat.Id,
                            inputOnlineFile,
                            thumb: null,
                            caption?.ToString(),
                            parseMode: ParseMode.Html,
                            replyMarkup: replyMarkup,
                            replyToMessageId: replyToMsgId
                        );
                    }

                    break;
            }
        }

        /// <summary>
        /// Delete message by MessageId. If no parameter, Bot will delete last sent Message
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="delaySecond"></param>
        protected async Task DeleteMessageAsync(
            int messageId = 0,
            int delaySecond = 0
        )
        {
            if (SentMessage == null)
                messageId = Message.MessageId;
            else if (messageId == 0)
                messageId = SentMessage.MessageId;

            await Task.Delay(TimeSpan.FromSeconds(delaySecond));

            Log.Information(
                "Deleting message on ChatId '{ChatId}' with messageId '{MessageId}'",
                ChatId,
                messageId
            );
            await Bot.DeleteMessageAsync(ChatId, messageId);
        }

        /// <summary>
        /// Downloads the file and save to file with the specified prefix name
        /// </summary>
        /// <param name="prefixName">The prefix name</param>
        /// <returns>The file name</returns>
        protected async Task<string> DownloadFileAsync(string prefixName)
        {
            var fileId = Message.GetFileId();
            if (fileId.IsNullOrEmpty()) fileId = Message.ReplyToMessage.GetFileId();

            Log.Information("Getting file by FileId {FileId}", fileId);
            var file = await Bot.GetFileAsync(fileId);

            var filePath = file.FilePath;
            Log.Debug("DownloadFile: {@File}", file);
            var fileName = $"{prefixName}_{filePath}";

            fileName = $"Storage/Caches/{fileName}".EnsureDirectory();

            await using var fileStream = SystemFile.OpenWrite(fileName);
            await Bot.DownloadFileAsync(file.FilePath, fileStream);
            Log.Information("File saved to {FileName}", fileName);

            return fileName;
        }

        /// <summary>
        /// Determine Message too old for reply
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        protected bool IsMessageOlderThan(TimeSpan time)
        {
            var expiry = DateTime.Now.Add(-time);
            var msgDate = RawUpdate.Message.Date.ToLocalTime();
            var diff = (expiry - msgDate).TotalSeconds;
            var messageId = RawUpdate.Message.MessageId;

            var isOld = diff > 0;

            Log.Debug(
                "On ChatId '{ChatId}', MessageId '{MessageId}' is older than time '{Time}'? {IsOld}",
                Chat.Id,
                messageId,
                time,
                isOld
            );

            return isOld;
        }
    }
}