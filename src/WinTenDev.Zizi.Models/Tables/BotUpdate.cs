using System;
using RepoDb.Attributes;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.RepoDb;

namespace WinTenDev.Zizi.Models.Tables;

[Map("bot_update")]
public class BotUpdate
{
    [Primary]
    public long Id { get; set; }

    [Map("bot_name")]
    public string BotName { get; set; }

    [Map("chat_id")]
    public long ChatId { get; set; }

    [Map("user_id")]
    public long UserId { get; set; }

    [PropertyHandler(typeof(UpdatePropertyHandler))]
    public Update Update { get; set; }

    [Map("created_at")]
    public DateTime CreatedAt { get; set; }
}