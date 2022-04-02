using System;
using System.Threading.Tasks;
using MoreLinq;
using Serilog;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

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
            var message = telegramService.MessageOrEdited;
            if (message == null) return false;

            var messageId = message.MessageId;
            var chatSettings = await telegramService.GetChatSetting();

            if (!chatSettings.EnableWordFilterGroupWide)
            {
                Log.Debug("Word Filter on {ChatId} is disabled!", chatId);
                return false;
            }

            var text = telegramService.MessageOrEditedText ?? telegramService.MessageOrEditedCaption;

            if (text.IsNullOrEmpty())
            {
                Log.Information("No Text at MessageId {MessageId} for scan..", messageId);
                return false;
            }

            if (telegramService.IsFromSudo &&
                (
                    text.StartsWith("/dkata") ||
                    text.StartsWith("/delkata") ||
                    text.StartsWith("/kata")))
            {
                Log.Debug("Seem User will modify Kata!");
                return false;
            }

            var result = await telegramService.WordFilterService.IsMustDelete(text);
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

            var note = "Pesan di Obrolan di hapus karena terdeteksi filter Kata.\n" + result.Notes;
            await telegramService.SendEventLogAsync(note, withForward: true);

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
}