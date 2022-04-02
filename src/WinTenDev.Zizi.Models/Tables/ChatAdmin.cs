using System;
using RepoDb.Attributes;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Models.Tables;

[Map("chat_admin")]
public class ChatAdmin
{
    [Primary]
    [Map("id")]
    public long Id { get; set; }

    [Map("chat_id")]
    public long ChatId { get; set; }

    [Map("user_id")]
    public long UserId { get; set; }

    [Map("role")]
    public ChatMemberStatus Role { get; set; }

    [Map("created_at")]
    public DateTime CreatedAt { get; set; }
}