using TL;
using User=Telegram.Bot.Types.User;

namespace WinTenDev.Zizi.Models.Types;

public class UserParticipant
{
    public long UserId { get; set; }
    public User User { get; set; }
    public ChannelParticipantBase ChannelParticipantBase { get; set; }
}