using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TL;
using WinTenDev.Zizi.Models.Interfaces;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
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

    public async Task<Channels_ChannelParticipants> GetAllParticipants(long chatId, ChannelParticipantsFilter channelParticipantsFilter = null)
    {
        var channelId = chatId.ReduceChatId();

        var cacheKey = MethodBase.GetCurrentMethod().CreateCacheKey(channelId);

        var channelParticipants = await _cacheService.GetOrSetAsync(cacheKey, async () => {
            var chats = await _client.Messages_GetAllChats(null);
            var channel = (Channel) chats.chats[1234567890];// the channel we want
            // var selectedChat = (Channel) chats.chats.Values.First(chat => chat.ID == channelId);
            var allParticipants = await _client.Channels_GetAllParticipants(channel);

            return allParticipants;
        });

        return channelParticipants;
    }
}