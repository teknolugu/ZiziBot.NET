using TL;

namespace WinTenDev.Zizi.Models.Telegram;

public class ChannelParticipants
{
    public Channels_ChannelParticipants ParticipantCreator { get; set; }
    public Channels_ChannelParticipants ParticipantAdmin  { get; set; }
}