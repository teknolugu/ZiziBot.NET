using System.Collections.Generic;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.Types;

public class ChannelSubscriptionIntoAddedChannelResult
{
    public bool IsSubscribedToAll { get; set; }
    public List<ChannelSubscriptionResult> ChannelSubscriptions { get; set; } = new();
}

public class ChannelSubscriptionIntoChannelResult
{
    public long ChannelId { get; set; }
    public ChatMember ChatMember { get; set; }
}

public class ChannelSubscriptionResult
{
    public bool IsSubscribed { get; set; }
    public long ChannelId { get; set; }
    public string ChannelName { get; set; }
    public string InviteLink { get; set; }
}