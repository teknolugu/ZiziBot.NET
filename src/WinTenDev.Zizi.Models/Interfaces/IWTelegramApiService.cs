using System.Threading.Tasks;
using TL;

namespace WinTenDev.Zizi.Models.Interfaces;

/// <summary>
/// The iw telegram api service interface
/// </summary>
public interface IWTelegramApiService
{
    /// <summary>
    /// Creates the client
    /// </summary>
    /// <returns>A task containing the client</returns>
    public Task<Channels_ChannelParticipants> GetAllParticipants(long chatId, ChannelParticipantsFilter channelParticipantsFilter = null);
}