using System;
using System.Linq;
using MoreLinq;
using Serilog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Utils.Telegram;

public static class MessageUtil
{
    public static string GetFileId(this Message message)
    {
        var fileId = "";

        switch (message.Type)
        {
            case MessageType.Document:
                fileId = message.Document?.FileId;
                break;

            case MessageType.Photo:
                fileId = message.Photo?.Last().FileId;
                break;

            case MessageType.Video:
                fileId = message.Video?.FileId;
                break;
        }

        return fileId;
    }

    public static string GetReducedFileId(this Message message)
    {
        return GetFileId(message).Substring(0, 17);
    }

    public static string GetTextWithoutCmd(
        this string message,
        bool withoutCmd = true
    )
    {
        var partsMsg = message.Split(' ');
        var text = message;

        if (withoutCmd && message.StartsWith("/", StringComparison.CurrentCulture))
        {
            text = message.TrimStart(partsMsg[0].ToCharArray());
        }

        return text.Trim();
    }

    public static string GetMessageLink(this Message message)
    {
        var chatUsername = message.Chat.Username;
        var messageId = message.MessageId;

        var messageLink = $"https://t.me/{chatUsername}/{messageId}";

        if (chatUsername.IsNullOrEmpty())
        {
            var trimmedChatId = message.Chat.Id.ReduceChatId();
            messageLink = $"https://t.me/c/{trimmedChatId}/{messageId}";
        }

        Log.Debug("MessageLink: {MessageLink}", messageLink);
        return messageLink;
    }

    public static string ParseWebHookInfo(this WebhookInfo webhookInfo)
    {
        var webhookInfoStr = "\n\n<i>Bot run in WebHook mode.</i>" +
                             $"\nUrl WebHook: {webhookInfo.Url}" +
                             $"\nUrl Custom Cert: {webhookInfo.HasCustomCertificate}" +
                             $"\nAllowed Updates: {webhookInfo.AllowedUpdates}" +
                             $"\nPending Count: {(webhookInfo.PendingUpdateCount - 1)}" +
                             $"\nMax Connection: {webhookInfo.MaxConnections}" +
                             $"\nLast Error: {webhookInfo.LastErrorDate:yyyy-MM-dd}" +
                             $"\nError Message: {webhookInfo.LastErrorMessage}";

        return webhookInfoStr;
    }

    public static string CloneText(this Message message)
    {
        if (message.ReplyToMessage != null) message = message.ReplyToMessage;

        Log.Debug("Clone text from MessageId: {MessageId}", message.MessageId);

        var entities = message.Entities ?? message.CaptionEntities;
        var entitiesValue = message.EntityValues ?? message.CaptionEntityValues;
        var messageText = message.Text ?? message.Caption;

        if (messageText == null) return string.Empty;
        if (entities == null) return messageText;

        entities.ForEach(
            (
                entity,
                index
            ) => {
                var oldValue = entitiesValue?.ElementAtOrDefault(index);

                if (oldValue == null) return;

                var newValue = oldValue;

                switch (entity.Type)
                {
                    case MessageEntityType.TextLink:
                        var url = entity.Url;
                        newValue = oldValue.MkUrl(url);
                        break;

                    case MessageEntityType.Mention:
                        break;
                    case MessageEntityType.Hashtag:
                        break;
                    case MessageEntityType.BotCommand:
                        break;
                    case MessageEntityType.Url:
                        break;
                    case MessageEntityType.Email:
                        break;
                    case MessageEntityType.Bold:
                        newValue = "<b>" + oldValue + "</b>";
                        break;
                    case MessageEntityType.Italic:
                        newValue = "<i>" + oldValue + "</i>";
                        break;
                    case MessageEntityType.Code:
                        newValue = "<code>" + oldValue + "</code>";
                        break;
                    case MessageEntityType.Pre:
                        break;
                    case MessageEntityType.TextMention:
                        break;
                    case MessageEntityType.PhoneNumber:
                        break;
                    case MessageEntityType.Cashtag:
                        break;
                    case MessageEntityType.Underline:
                        newValue = "<u>" + oldValue + "</u>";
                        break;
                    case MessageEntityType.Strikethrough:
                        newValue = "<s>" + oldValue + "</s>";
                        break;
                    default:
                        Log.Warning("No action for entity type: {EntityType}", entity.Type);
                        break;
                }

                messageText = messageText.Replace(oldValue, newValue);
            }
        );

        return messageText;
    }
}