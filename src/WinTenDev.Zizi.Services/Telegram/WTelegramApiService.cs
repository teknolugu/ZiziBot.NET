using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TL;
using WinTenDev.Zizi.Models.Interfaces;
using WinTenDev.Zizi.Models.Telegram;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WTelegram;

namespace WinTenDev.Zizi.Services.Telegram;

public class WTelegramApiService : IWTelegramApiService
{
    private readonly ILogger<WTelegramApiService> _logger;
    private readonly CacheService _cacheService;
    private readonly Client _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WTelegramApiService"/> class
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="cacheService"></param>
    /// <param name="client">WTelegram client</param>
    public WTelegramApiService(
        ILogger<WTelegramApiService> logger,
        CacheService cacheService,
        Client client
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _client = client;
    }

    private async Task<Channel> GetChannel(long chatId)
    {
        var channelId = chatId.ReduceChatId();

        var chats = await _client.Messages_GetAllChats(null);
        var channel = (Channel) chats.chats.Values.FirstOrDefault(chat => chat.ID == channelId);

        return channel;
    }

    public async Task<Channels_ChannelParticipants> GetAllParticipants(
        long chatId,
        ChannelParticipantsFilter channelParticipantsFilter = null
    )
    {
        var channelId = chatId.ReduceChatId();

        var cacheKey = MethodBase.GetCurrentMethod().CreateCacheKey(channelId);

        var channelParticipants = await _cacheService.GetOrSetAsync(
            cacheKey: cacheKey,
            action: async () => {
                var chats = await _client.Messages_GetAllChats(null);
                // var channel = (Channel) chats.chats[1234567890];// the channel we want
                var channel = (Channel) chats.chats.Values.First(chat => chat.ID == channelId);
                var allParticipants = await _client.Channels_GetAllParticipants(channel);

                return allParticipants;
            }
        );

        return channelParticipants;
    }

    public async Task<Channels_ChannelParticipants> GetAllParticipantsCore(long chatId)
    {
        var channelId = chatId.ReduceChatId();

        var chats = await _client.Messages_GetAllChats(null);
        var channel = (Channel) chats.chats.Values.FirstOrDefault(chat => chat.ID == channelId);

        var allParticipants = await _client.Channels_GetAllParticipants(channel);
        return allParticipants;
    }

    public async Task<ChannelParticipants> GetChatAdministratorsCore(long chatId)
    {
        var channel = await GetChannel(chatId);

        var channelsParticipants = await _client.Channels_GetParticipants(
            channel: channel,
            filter: new ChannelParticipantsAdmins(),
            offset: 0,
            limit: 0,
            hash: 0
        );

        var participantCreator = channelsParticipants.participants
            .Where(x => x.GetType() == typeof(ChannelParticipantCreator))
            .Select(x => x as ChannelParticipantCreator);

        var participantAdmins = channelsParticipants.participants
            .Where
            (
                x =>
                    x.GetType() == typeof(ChannelParticipantAdmin) ||
                    x.UserID != participantCreator.FirstOrDefault().UserID
            )
            .Select(x => x as ChannelParticipantAdmin);

        var participants = new ChannelParticipants()
        {
            ParticipantCreator = new Channels_ChannelParticipants()
            {
                participants = participantCreator.ToArray(),
                users = channelsParticipants.users.Where(x => x.Value.ID == participantCreator.FirstOrDefault()?.UserID)
                    .ToDictionary(x => x.Key, x => x.Value)
            },
            ParticipantAdmin = new Channels_ChannelParticipants()
            {
                participants = participantAdmins.ToArray(),
                users = channelsParticipants.users.Where(x => x.Value.ID != participantCreator.FirstOrDefault()?.UserID)
                    .Where(x => participantAdmins.Any(y => y.UserID == x.Value.ID))
                    .ToDictionary(x => x.Key, x => x.Value)
            }
        };

        return participants;
    }

    public async Task<List<int>> GetMessagesIdByUserId(
        long chatId,
        long userId,
        int lastMessageId
    )
    {
        _logger.LogInformation(
            "Deleting messages from UserId {UserId} in ChatId {ChatId}",
            userId,
            chatId
        );

        var offset = 200;
        var channel = await GetChannel(chatId);

        var messageRanges = Enumerable
            .Range(lastMessageId - offset, offset)
            .Reverse()
            .Select(id => new InputMessageID() { id = id })
            .Cast<InputMessage>()
            .ToArray();

        var allMessages = await _client.Channels_GetMessages(channel, messageRanges);
        var filteredMessage = allMessages.Messages
            .Where(messageBase => messageBase.GetType() == typeof(Message))
            .Where(messageBase => messageBase.From.ID == userId);
        var messageIds = filteredMessage.Select(messageBase => messageBase.ID);

        return messageIds.ToList();
    }

    public async Task DeleteMessageByUserId(
        long chatId,
        long userId,
        int lastMessageId
    )
    {
        try
        {
            _logger.LogInformation(
                "Deleting messages from UserId {UserId} in ChatId {ChatId}",
                userId,
                chatId
            );

            var channel = await GetChannel(chatId);
            var messageIds = await GetMessagesIdByUserId(
                chatId,
                userId,
                lastMessageId
            );

            var deleteMessages = await _client.Channels_DeleteMessages(channel, messageIds.ToArray());

            _logger.LogDebug("Deleted {@AffectedHistory} messages", deleteMessages);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error deleting messages from UserId {UserId} in ChatId {ChatId}",
                userId,
                chatId
            );
        }
    }
}
