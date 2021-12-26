using System;
using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types;

public class RssSetting
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("chat_id")]
    public long ChatId { get; set; }

    [JsonProperty("from_id")]
    public long FromId { get; set; }

    [JsonProperty("url_feed")]
    public string UrlFeed { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}