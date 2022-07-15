using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Telegram;

public class EventLogService
{
    private readonly EventLogConfig _eventLogConfig;
    private readonly ITelegramBotClient _botClient;
    private readonly ChatService _chatService;
    private readonly SettingsService _settingsService;

    public EventLogService(
        IOptionsSnapshot<EventLogConfig> eventLogConfig,
        ITelegramBotClient botClient,
        ChatService chatService,
        SettingsService settingsService
    )
    {
        _eventLogConfig = eventLogConfig.Value;
        _botClient = botClient;
        _chatService = chatService;
        _settingsService = settingsService;
    }

    private async Task<List<long>> GetEventLogTargets(
        long chatId,
        bool sendGlobalOnly = false
    )
    {
        var currentSetting = await _settingsService.GetSettingsByGroup(chatId);

        var globalLogTarget = _eventLogConfig.ChannelId;
        var chatLogTarget = currentSetting.EventLogChatId;

        var eventLogTargets = new List<long>()
        {
            globalLogTarget,
            sendGlobalOnly ? -1 : chatLogTarget
        };

        var filteredTargets = eventLogTargets
            .Where(x => x < 0)
            .ToList();

        Log.Debug("List Channel Targets: {ListLogTarget}", filteredTargets);

        return filteredTargets;
    }

    public async Task SendEventLogAsync(
        Message message,
        long chatId = -1,
        string text = "N/A",
        MessageFlag messageFlag = MessageFlag.General,
        int forwardMessageId = -1,
        bool deleteForwardedMessage = false,
        bool sendGlobalOnly = false
    )
    {
        Log.Information("Preparing send EventLog");
        // var chat = await _chatService.GetChatAsync(chatId);
        // var chatMember = await _chatService.GetChatMemberAsync(chatId, userId);
        // var eventLogSendDto = new EventLogSendDto()
        // {
        //     Chat = chat,
        //     User = chatMember.User
        // };

        var fromNameLink = message.From.GetNameLink();
        var chatNameLink = message.Chat.GetChatNameLink();
        var fromId = message.From!.Id;
        var reducedChatId = message.Chat.Id.ReduceChatId();
        var messageLink = message.GetMessageLink();

        var sendLog = "🐾 <b>EventLog Preview</b>" +
                      $"\n<b>Chat:</b> <code>{reducedChatId}</code> - {chatNameLink}" +
                      $"\n<b>User:</b> <code>{fromId}</code> - {fromNameLink}" +
                      $"\n<b>Flag:</b> #{messageFlag}" +
                      $"\n<b>Note:</b> {text}" +
                      $"\n<a href='{messageLink}'>Go to Message</a>" +
                      $"\n#{message.Type} #U{fromId} #C{reducedChatId}";

        await SendEventLogCoreAsync(
            sendText: sendLog,
            chatId: chatId,
            disableWebPreview: true,
            forwardMessageId: forwardMessageId,
            deleteForwardedMessage: deleteForwardedMessage,
            sendGlobalOnly: sendGlobalOnly
        );
    }

    public async Task SendEventLogCoreAsync(
        string sendText,
        long chatId = -1,
        bool disableWebPreview = false,
        int replyToMessageId = -1,
        int forwardMessageId = -1,
        bool deleteForwardedMessage = false,
        bool sendGlobalOnly = false
    )
    {
        var eventLogTargets = await GetEventLogTargets(chatId, sendGlobalOnly);

        foreach (var eventLogTarget in eventLogTargets)
        {
            try
            {
                if (forwardMessageId > 0)
                {
                    var forwardMessage = await _botClient.ForwardMessageAsync(
                        chatId: eventLogTarget,
                        fromChatId: chatId,
                        messageId: forwardMessageId
                    );

                    replyToMessageId = forwardMessage.MessageId;

                    if (deleteForwardedMessage)
                    {
                        await _botClient.DeleteMessageAsync(
                            chatId: chatId,
                            messageId: forwardMessageId
                        );
                    }
                }
            }
            catch (Exception forwardMessageException)
            {
                Log.Warning(
                    "Fail forward MessageId: {MessageId} at ChatId: {ChatId}. Message: {Message}",
                    forwardMessageId,
                    chatId,
                    forwardMessageException.Message
                );
            }

            try
            {
                var sentEventLog = await _botClient.SendTextMessageAsync(
                    chatId: eventLogTarget,
                    text: sendText,
                    allowSendingWithoutReply: true,
                    disableWebPagePreview: disableWebPreview,
                    replyToMessageId: replyToMessageId,
                    parseMode: ParseMode.Html
                );

                Log.Information(
                    "Send EventLog Successfully to ChatId: {ChatId}, Sent MessageId: {MessageId}",
                    eventLogTarget,
                    sentEventLog.MessageId
                );
            }
            catch (Exception sendEventLogException)
            {
                Log.Error(
                    sendEventLogException,
                    "Fail send EventLog to ChatId: {ChatId}",
                    eventLogTarget
                );
            }
        }
    }
}