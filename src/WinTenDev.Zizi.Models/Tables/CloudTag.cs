using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Tables;

[Table("tags")]
public class CloudTag
{
    public long Id { get; set; }

    [SqlKata.Column("chat_id")]
    [JsonProperty("chat_id")]
    public long ChatId { get; set; }

    [SqlKata.Column("from_id")]
    [JsonProperty("from_id")]
    public long FromId { get; set; }

    [SqlKata.Column("tag")]
    [JsonProperty("tag")]
    public string Tag { get; set; }

    [SqlKata.Column("content")]
    [JsonProperty("content")]
    public string Content { get; set; }

    [SqlKata.Column("btn_data")]
    [JsonProperty("btn_data")]
    public string BtnData { get; set; }

    [SqlKata.Column("type_data")]
    [JsonProperty("type_data")]
    public MediaType TypeData { get; set; }

    [SqlKata.Column("file_id")]
    [JsonProperty("file_id")]
    public string FileId { get; set; }

    [SqlKata.Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [SqlKata.Column("updated_at")]
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}