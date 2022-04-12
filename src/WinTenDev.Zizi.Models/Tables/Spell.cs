using System;
using RepoDb.Attributes;

namespace WinTenDev.Zizi.Models.Tables;

[Map("spells")]
public class Spell
{
    [Primary]
    [Map("id")]
    public long Id { get; set; }

    [Map("typo")]
    public string Typo { get; set; }

    [Map("fix")]
    public string Fix { get; set; }

    [Map("from_id")]
    public long FromId { get; set; }

    [Map("chat_id")]
    public long ChatId { get; set; }

    [Map("created_at")]
    public DateTime CreatedAt { get; set; }
}
