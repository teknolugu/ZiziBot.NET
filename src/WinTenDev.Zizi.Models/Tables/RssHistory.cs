using System;
using Newtonsoft.Json;
using SqlKata;

namespace WinTenDev.Zizi.Models.Tables;

public class RssHistory
{
    [JsonProperty("id")]
    [Column("id")]
    public int Id { get; set; }

    [JsonProperty("chat_id")]
    [Column("chat_id")]
    public long ChatId { get; set; }

    [JsonProperty("rss_source")]
    [Column("rss_source")]
    public string RssSource { get; set; }

    [JsonProperty("title")]
    [Column("title")]
    public string Title { get; set; }

    [JsonProperty("url")]
    [Column("url")]
    public string Url { get; set; }

    [JsonProperty("publish_date")]
    [Column("publish_date")]
    public DateTimeOffset PublishDate { get; set; }

    [JsonProperty("author")]
    [Column("author")]
    public string Author { get; set; }

    [JsonProperty("created_at")]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}