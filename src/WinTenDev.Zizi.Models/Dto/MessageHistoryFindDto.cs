using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Dto;

public class MessageHistoryFindDto
{
    public long MessageId { get; set; }
    public MessageFlag MessageFlag { get; set; }
}