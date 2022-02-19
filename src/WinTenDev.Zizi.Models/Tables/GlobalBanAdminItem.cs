using System;
using Newtonsoft.Json;
using SqlKata;

namespace WinTenDev.Zizi.Models.Tables;

public class GlobalBanAdminItem
{
    [SqlKata.Ignore]
    [JsonProperty("id")]
    public long Id { get; set; }

    [Column("user_id")]
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [Column("username")]
    [JsonProperty("username")]
    public string Username { get; set; }

    [Column("promoted_by")]
    [JsonProperty("promoted_by")]
    public long PromotedBy { get; set; }

    [Column("promoted_from")]
    [JsonProperty("promoted_from")]
    public long PromotedFrom { get; set; }

    [Column("is_banned")]
    [JsonProperty("is_banned")]
    public bool IsBanned { get; set; }

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
}