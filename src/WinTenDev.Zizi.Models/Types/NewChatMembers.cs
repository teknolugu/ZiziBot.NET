using System.Collections.Generic;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.Types;

public class NewChatMembers
{
    public IEnumerable<User> AllNewChatMembers { get; set; }
    public IEnumerable<User> NewNoUsernameChatMembers { get; set; }
    public IEnumerable<User> NewBotChatMembers { get; set; }
    public List<User> NewPassedChatMembers { get; set; } = new();
    public List<User> NewKickedChatMembers { get; set; } = new();

    public IEnumerable<string> AllNewChatMembersStr { get; set; }
    public IEnumerable<string> NewPassedChatMembersStr { get; set; }
    public IEnumerable<string> NewNoUsernameChatMembersStr { get; set; }
    public IEnumerable<string> NewBotChatMembersStr { get; set; }
}