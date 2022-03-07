using System.Threading.Tasks;
using MoreLinq;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Telegram.Extensions;

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
}