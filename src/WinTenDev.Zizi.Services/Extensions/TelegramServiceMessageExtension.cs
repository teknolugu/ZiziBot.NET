using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;
using QRCodeDecoderLibrary;
using Serilog;
using SerilogTimings;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Image=SixLabors.ImageSharp.Image;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceMessageExtension
{
    public static async Task<Message> SendMessageTextAsync(
        this TelegramService telegramService,
        MessageResponseDto messageResponseDto
    )
    {
        var sendMessageText = await telegramService.SendTextMessageAsync(
            sendText: messageResponseDto.MessageText,
            replyMarkup: messageResponseDto.ReplyMarkup,
            replyToMsgId: messageResponseDto.ReplyToMessageId.ToInt(),
            disableWebPreview: messageResponseDto.DisableWebPreview
        );

        if (messageResponseDto.ScheduleDeleteAt == default) return sendMessageText;

        var currentCommand = telegramService.GetCommand(withoutSlash: true);
        var commandFlag = currentCommand.ToEnum(defaultValue: MessageFlag.General);

        if (messageResponseDto.IncludeSenderForDelete)
            telegramService.SaveSenderMessageToHistory(commandFlag, messageResponseDto.ScheduleDeleteAt);

        telegramService.SaveSentMessageToHistory(commandFlag, messageResponseDto.ScheduleDeleteAt);

        return sendMessageText;
    }

    public static async Task<Message> EditMessageTextAsync(
        this TelegramService telegramService,
        MessageResponseDto messageResponseDto
    )
    {
        var sendMessageText = await telegramService.EditMessageTextAsync(
            sendText: messageResponseDto.MessageText,
            replyMarkup: messageResponseDto.ReplyMarkup,
            disableWebPreview: messageResponseDto.DisableWebPreview
        );

        if (messageResponseDto.ScheduleDeleteAt == default) return sendMessageText;

        var currentCommand = telegramService.GetCommand(withoutSlash: true);
        var commandFlag = currentCommand.ToEnum(defaultValue: MessageFlag.General);

        await telegramService.MessageHistoryService.DeleteMessageHistoryAsync(
            new MessageHistoryFindDto()
            {
                ChatId = sendMessageText.Chat.Id,
                MessageId = sendMessageText.MessageId
            }
        );

        if (messageResponseDto.IncludeSenderForDelete)
            telegramService.SaveSenderMessageToHistory(commandFlag, messageResponseDto.ScheduleDeleteAt);

        telegramService.SaveSentMessageToHistory(commandFlag, messageResponseDto.ScheduleDeleteAt);

        return sendMessageText;
    }

    public static async Task<RequestResult> SendMediaGroupAsync(
        this TelegramService telegramService,
        MessageResponseDto messageResponseDto
    )
    {
        var requestResult = await telegramService.SendMediaGroupAsync(
            listAlbum: messageResponseDto.ListAlbum
        );

        if (messageResponseDto.ScheduleDeleteAt == default) return requestResult;

        var currentCommand = telegramService.GetCommand(withoutSlash: true);
        var commandFlag = currentCommand.ToEnum(defaultValue: MessageFlag.General);

        requestResult.SentMessages.ForEach(
            message => {
                var messageId = message.MessageId;

                if (messageResponseDto.IncludeSenderForDelete)
                {
                    telegramService.SaveMessageToHistoryAsync(
                        messageId: messageId,
                        messageFlag: commandFlag,
                        deleteAt: messageResponseDto.ScheduleDeleteAt
                    ).InBackground();
                }

                telegramService.SaveMessageToHistoryAsync(
                    messageId: messageId,
                    messageFlag: commandFlag,
                    deleteAt: messageResponseDto.ScheduleDeleteAt
                ).InBackground();
            }
        );

        return requestResult;
    }

    public static async Task<bool> ScanMessageAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;

        try
        {
            if (telegramService.IsGlobalIgnored()) return false;

            var message = telegramService.MessageOrEdited;
            var eventLogService = telegramService.GetRequiredService<EventLogService>();
            var wordFilterService = telegramService.GetRequiredService<WordFilterService>();

            if (message == null) return false;

            var messageId = message.MessageId;
            var chatSettings = await telegramService.GetChatSetting();

            if (!chatSettings.EnableWordFilterGroupWide)
            {
                Log.Debug("Word Filter on {ChatId} is disabled!", chatId);
                return false;
            }

            if (await telegramService.CheckFromAdminOrAnonymous())
            {
                Log.Debug("Scan Message disabled for Administrator. ChatId: {ChatId}", chatId);
                return false;
            }

            var textToScan = telegramService.MessageOrEdited.CloneText(true);

            var scanMedia = await telegramService.ScanMediaAsync();
            textToScan += "\n\n" + scanMedia;

            if (telegramService.IsFromSudo &&
                (
                    textToScan.StartsWith("/dkata") ||
                    textToScan.StartsWith("/delkata") ||
                    textToScan.StartsWith("/kata")))
            {
                Log.Debug("Seem User will modify Kata!");
                return false;
            }

            var result = await wordFilterService.IsMustDelete(textToScan);
            var isShouldDelete = result.IsSuccess;

            if (isShouldDelete)
                Log.Information("Starting scan image if available..");

            Log.Information(
                "Message {MsgId} IsMustDelete: {IsMustDelete}",
                messageId,
                isShouldDelete
            );

            if (!isShouldDelete) return false;

            Log.Debug(
                "Scan Message at ChatId: {ChatId}. Result: {@V}",
                chatId,
                result
            );

            var note = "Pesan dihapus karena terdeteksi filter Kata." +
                       $"\n{result.Notes}";

            await eventLogService.SendEventLogAsync(
                chatId: chatId,
                message: message,
                text: note,
                messageFlag: MessageFlag.BadWord,
                forwardMessageId: messageId,
                deleteForwardedMessage: true
            );

            await telegramService.DeleteAsync(messageId);

            return true;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error occured when Scan Message at ChatId {ChatId}",
                chatId
            );

            return false;
        }
    }

    public static async Task<string> ScanMediaAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        try
        {
            var op = Operation.Begin("Scanning Media from ChatId: {ChatId}", chatId);

            var message = telegramService.MessageOrEdited;

            if (message.Document == null &&
                message.Photo == null)
            {
                return string.Empty;
            }

            var qrDecoder = telegramService.GetRequiredService<QRDecoder>();

            var qrFile = await telegramService.DownloadFileAsync("qr-reader");
            var image = await Image.LoadAsync(qrFile);
            var qrResult = qrDecoder.ImageDecoder(image);
            var data = QRDecoder.ByteArrayToString(qrResult.FirstOrDefault());

            DirUtil.CleanCacheFiles(
                s =>
                    s.Contains(chatId.ReduceChatId().ToString()) &&
                    s.Contains("qr-reader")
            );

            op.Complete();

            return data;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error occured when Scan Media at ChatId {ChatId}",
                chatId
            );

            return string.Empty;
        }
    }

    public static async Task<bool> CheckUpdateHistoryAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;

        if (telegramService.IsPrivateGroup ||
            telegramService.IsChannel ||
            telegramService.IsGlobalIgnored())
        {
            Log.Debug("Check Update History not available for ChatId: {ChatId}", chatId);
            return false;
        }

        try
        {
            var chatSettings = await telegramService.GetChatSetting();
            if (chatSettings.EnablePrivacyMode)
            {
                Log.Debug("Check Update History disabled for ChatId: {ChatId} because Privacy Mode is enabled", chatId);
                return false;
            }

            var message = telegramService.MessageOrEdited;
            var messageEntities = message?.Entities ?? message?.CaptionEntities;

            if (messageEntities == null) return false;

            var filteredEntities = messageEntities?.Where(
                x =>
                    x.Type is MessageEntityType.Mention or MessageEntityType.Url
            ).ToList();

            var botUpdateService = telegramService.GetRequiredService<BotUpdateService>();

            var isUpdateExist = await botUpdateService.IsBotUpdateExistAsync(chatId, fromId);
            if (!(filteredEntities?.Count > 0) || isUpdateExist)
                return false;

            var mentionAdmin = await telegramService.GetMentionAdminsStr();
            var fullName = telegramService.From.GetFullName();

            var htmlMessage = HtmlMessage.Empty
                .BoldBr("Anti-Spam detection Beta")
                .Bold("UserId: ").CodeBr(fromId.ToString())
                .Bold("Name: ").CodeBr(fullName)
                .Text("Telah mengirimkan link atau mention untuk pesan pertamanya. Apakah ini Spam?")
                .Text(mentionAdmin);

            var inlineKeyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Ya, ini Spam!", $"gban add {fromId}"),
                        InlineKeyboardButton.WithCallbackData("Ini bukan Spam", $"gban del {fromId}")
                    }
                }
            );

            await telegramService.SendTextMessageAsync(
                sendText: htmlMessage.ToString(),
                replyMarkup: inlineKeyboard,
                scheduleDeleteAt: DateTime.UtcNow.AddDays(1),
                preventDuplicateSend: true,
                messageFlag: MessageFlag.SpamDetection
            );

            return true;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error occured when Check Update History at ChatId {ChatId}",
                chatId
            );

            return false;
        }
    }

    public static async Task PinMessageAsync(this TelegramService telegramService)
    {
        var client = telegramService.Client;
        var message = telegramService.MessageOrEdited;
        var chatId = telegramService.ChatId;

        await telegramService.DeleteSenderMessageAsync();

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            Log.Warning("Pin message only for Admin on Current Chat");
            return;
        }

        if (message.ReplyToMessage == null)
        {
            // var messageId = message.ReplyToMessage.MessageId;
            //
            // await client.UnpinChatMessageAsync(chatId, messageId);
            // await client.PinChatMessageAsync(chatId, messageId);

            await telegramService.SendTextMessageAsync(
                sendText: "Balas pesan yang akan di pin",
                replyToMsgId: message.MessageId,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                preventDuplicateSend: true
            );

            return;
        }

        var replyToMessage = telegramService.ReplyToMessage;
        var replyToMessageId = replyToMessage.MessageId;

        var inlineKeyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔔 Beri tahu", $"pin-message {replyToMessageId} notify"),
                    InlineKeyboardButton.WithCallbackData("🔕 Senyap", $"pin-message {replyToMessageId} silent")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Tutup", $"delete-message current-message")
                }
            }
        );

        const string sendText = "Apakah Anda ingin beri tahu Anggota saat menyematkan pesan ini?";
        await telegramService.SendTextMessageAsync(
            sendText,
            replyToMsgId: message.MessageId,
            replyMarkup: inlineKeyboard,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
            preventDuplicateSend: true
        );
    }

    public static async Task PurgeMessageAsync(this TelegramService telegramService)
    {
        if (!await telegramService.CheckUserPermission())
        {
            Log.Information("User does not have permission to purge message");
            return;
        }

        var chatId = telegramService.ChatId;
        var userId = telegramService.FromId;
        var wTelegramService = telegramService.GetRequiredService<WTelegramApiService>();

        if (!await telegramService.CheckProbeRequirementAsync(true)) return;

        var replyToMessage = telegramService.ReplyToMessage;

        if (replyToMessage == null)
        {
            await telegramService.SendTextMessageAsync(
                enumLang: Purge.ReplyToPurge,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );

            return;
        }

        var startMessageId = telegramService.Message.MessageId;
        var endMessageId = replyToMessage.MessageId;
        var replyToUserId = replyToMessage.From.Id;
        var targetUserId = telegramService.IsCommand("/purge") ? replyToUserId : -1;
        var featureName = telegramService.IsCommand("/purge") ? "Purge Message" : "Purge Message Any";
        var onProgress = await telegramService.GetLocalizationString(Purge.OnProgress);

        var confirmationMessage = await telegramService.GetLocalizationString(Purge.ConfirmationMessage);

        if (replyToMessage.Date < DateTime.UtcNow.AddDays(-3))
        {
            await telegramService.SendTextMessageAsync(
                enumLang: Purge.MaxDateExceed,
                replyToMsgId: replyToMessage.MessageId,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );

            return;
        }

        var htmlMessage = HtmlMessage.Empty
            .BoldBr($"🧹 {featureName}")
            .TextBr(onProgress);
        await telegramService.AppendTextAsync(htmlMessage.ToString());

        var messages = await wTelegramService.GetAllMessagesAsync(
            chatId: chatId,
            startMessageId: startMessageId,
            endMessageId: endMessageId,
            userId: targetUserId
        );

        var messageIds = messages.Select(message => message.ID).ToList();

        startMessageId = messageIds.FirstOrDefault();
        endMessageId = messageIds.LastOrDefault();

        var messageLinkStart = replyToMessage.GetMessageLink(startMessageId);
        var messageLinkEnd = replyToMessage.GetMessageLink(endMessageId);

        var placeHolders = new List<(string placeholder, string value)>()
        {
            ("StartMessageLink", messageLinkStart),
            ("EndMessageLink", messageLinkEnd)
        };

        var featureDescription = telegramService.IsCommand("/purge")
            ? await telegramService.GetLocalizationString(Purge.PurgeDescription, placeHolders)
            : await telegramService.GetLocalizationString(Purge.PurgeAnyDescription, placeHolders);

        var inlineKeyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("🔔 Pesan awal", messageLinkStart),
                    InlineKeyboardButton.WithUrl("🔕 Pesan akhir", messageLinkEnd)
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Jalankan", $"delete-message purge {startMessageId} {endMessageId} {targetUserId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Tutup", $"delete-message current-message")
                }
            }
        );

        // await messageIds.AsyncParallelForEach(
        //     async i => {
        //         await telegramService.DeleteAsync(i);
        //     }
        // );

        htmlMessage.PopLine(2);

        if (targetUserId != -1)
        {
            htmlMessage.Bold("UserId: ").CodeBr(targetUserId.ToString());
        }

        htmlMessage
            // .Bold("Pesan awal: ").CodeBr(startMessageId.ToString())
            // .Bold("Pesan akhir: ").CodeBr(endMessageId.ToString())
            .Bold("Jumlah: ").CodeBr(messageIds.Count().ToString())
            .Text(featureDescription).Text(" ")
            .Text(confirmationMessage);

        await telegramService.EditMessageTextAsync(
            sendText: htmlMessage.ToString(),
            inlineKeyboard,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
            includeSenderMessage: true
        );
    }

    public static async Task DeleteMessageManyAsync(
        this TelegramService telegramService,
        long customUserId = -1
    )
    {
        var wTelegramService = telegramService.GetRequiredService<WTelegramApiService>();
        var chatId = telegramService.ChatId;
        var userId = customUserId == -1 ? telegramService.FromId : customUserId;
        var messageId = telegramService.AnyMessage.MessageId;

        var messageIds = await wTelegramService.GetMessagesIdByUserId(
            chatId: chatId,
            userId: userId,
            lastMessageId: messageId
        );

        Log.Debug(
            "Deleting {MessageIdsCount} Messages for UserId {UserId}",
            messageIds.Count,
            userId
        );

        await messageIds.AsyncParallelForEach(
            maxDegreeOfParallelism: 8,
            body: async id => {
                await telegramService.DeleteAsync(id);
            }
        );
    }
}