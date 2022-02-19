using System;

namespace WinTenDev.Zizi.Models.Tables;

public class BlockList
{
    public int Id { get; set; }
    public string UrlSource { get; set; }
    public long FromId { get; set; }
    public long ChatId { get; set; }
    public DateTime CreatedAt { get; set; }
}