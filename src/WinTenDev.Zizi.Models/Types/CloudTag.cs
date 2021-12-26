using System;
using Newtonsoft.Json;
using SqlKata;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Types;

public class CloudTag
{
    [Column("chat_id")]
    [JsonProperty("chat_id")]
    public long ChatId { get; set; }

    [Column("from_id")]
    [JsonProperty("from_id")]
    public long FromId { get; set; }

    [Column("tag")]
    [JsonProperty("tag")]
    public string Tag { get; set; }

    [Column("content")]
    [JsonProperty("content")]
    public string Content { get; set; }

    [Column("btn_data")]
    [JsonProperty("btn_data")]
    public string BtnData { get; set; }

    [Column("type_data")]
    [JsonProperty("type_data")]
    public MediaType TypeData { get; set; }

    [Column("file_id")]
    [JsonProperty("file_id")]
    public string FileId { get; set; }

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}