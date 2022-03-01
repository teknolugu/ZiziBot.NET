using System;

namespace WinTenDev.Zizi.Models.Dto;

public class MessageHistoryInsertDto
{
    public string MessageFlag { get; set; }
    public long FromId { get; set; }
    public long ChatId { get; set; }
    public long MessageId { get; set; }
    public DateTime DeleteAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}