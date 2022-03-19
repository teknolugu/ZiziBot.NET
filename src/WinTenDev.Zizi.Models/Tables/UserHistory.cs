using System;
using MongoDB.Bson;
using Newtonsoft.Json;
using Realms;

namespace WinTenDev.Zizi.Models.Tables;

public class UserHistory : RealmObject
{
    [JsonProperty("id")]
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [JsonProperty("via_bot")]
    public string ViaBot { get; set; }

    [JsonProperty("message_type")]
    public string UpdateType { get; set; }

    public DateTimeOffset MessageDate { get; set; }

    [JsonProperty("from_id")]
    public long FromId { get; set; }

    [JsonProperty("from_first_name")]
    public string FromFirstName { get; set; }

    [JsonProperty("from_last_name")]
    public string FromLastName { get; set; }

    [JsonProperty("from_username")]
    public string FromUsername { get; set; }

    [JsonProperty("from_lang_code")]
    public string FromLangCode { get; set; }

    [JsonProperty("chat_id")]
    public long ChatId { get; set; }

    [JsonProperty("chat_username")]
    public string ChatUsername { get; set; }

    [JsonProperty("chat_type")]
    public string ChatType { get; set; }

    [JsonProperty("chat_title")]
    public string ChatTitle { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}