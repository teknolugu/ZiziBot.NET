using System;
using Newtonsoft.Json;
using RepoDb.Attributes;
using SqlKata;

namespace WinTenDev.Zizi.Models.Tables;

[Map("global_bans")]
public class GlobalBanItem
{
    [Column("id")]
    [JsonProperty("id")]
    public long Id { get; set; }

    [Column("user_id")]
    [JsonProperty("user_id")]
    [Map("user_id")]
    public long BannedUserId { get; set; }

    [Column("chat_id")]
    [JsonProperty("chat_id")]
    [Map("chat_id")]
    public long ChatId { get; set; }

    [Column("from_id")]
    [JsonProperty("from_id")]
    [Map("from_id")]
    public long UserId { get; set; }

    [Column("reason_ban")]
    [JsonProperty("reason_ban")]
    [Map("reason")]
    public string ReasonBan { get; set; }

    [Column("banned_by")]
    [JsonProperty("banned_by")]
    public long BannedBy { get; set; }

    [Column("banned_from")]
    [JsonProperty("banned_from")]
    public long BannedFrom { get; set; }

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
}