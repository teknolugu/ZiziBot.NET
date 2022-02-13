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

        var channelParticipants = await _cacheService.GetOrSetAsync
        (
            cacheKey, async () => {
                var chats = await _client.Messages_GetAllChats(null);
                // var channel = (Channel) chats.chats[1234567890];// the channel we want
                var channel = (Channel) chats.chats.Values.First(chat => chat.ID == channelId);
                var allParticipants = await _client.Channels_GetAllParticipants(channel);

                return allParticipants;
            }
        );

        return channelParticipants;
    }

    public async Task<Channels_ChannelParticipants> GetAllParticipantsCore(
        long chatId
    )
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

        var channelsParticipants = await _client.Channels_GetParticipants
        (
            channel,
            new ChannelParticipantsAdmins(),
            0, 0, 0
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
                users = channelsParticipants.users.Where(x => x.Value.ID == participantCreator.FirstOrDefault().UserID)
                    .ToDictionary(x => x.Key, x => x.Value)
            },
            ParticipantAdmin = new Channels_ChannelParticipants()
            {
                participants = participantAdmins.ToArray(),
                users = channelsParticipants.users.Where(x => x.Value.ID != participantCreator.FirstOrDefault().UserID)
                    .Where(x => participantAdmins.Any(y => y.UserID == x.Value.ID))
                    .ToDictionary(x => x.Key, x => x.Value)
            }
        };

        return participants;
    }
}