using System;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Tables;

public class MessageHistory
{
    public long Id { get; set; }
    public MessageFlag MessageFlag { get; set; }
    public long FromId { get; set; }
    public long ChatId { get; set; }
    public long MessageId { get; set; }
    public DateTime DeleteAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}