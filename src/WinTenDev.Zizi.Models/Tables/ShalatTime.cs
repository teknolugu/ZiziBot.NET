using System;
using RepoDb.Attributes;

namespace WinTenDev.Zizi.Models.Tables;

[Map("shalat_time")]
public class ShalatTime
{
    [Primary]
    public long Id { get; set; }

    [Map("user_id")]
    public long UserId { get; set; }

    [Map("chat_id")]
    public long ChatId { get; set; }

    [Map("city_id")]
    public long CityId { get; set; }

    [Map("city_name")]
    public string CityName { get; set; }

    [Map("enable_notification")]
    public bool EnableNotification { get; set; }

    [Map("created_at")]
    public DateTime CreatedAt { get; set; }

    [Map("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
