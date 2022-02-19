using System;

namespace WinTenDev.Zizi.Models.Tables;

public class Afk
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public string AfkReason { get; set; }
    public bool IsAfk { get; set; }
    public DateTime AfkStart { get; set; }
    public DateTime AfkEnd { get; set; }
}