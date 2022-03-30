using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.Dto;

public class EventLogSendDto
{
    public User User { get; set; }
    public Chat Chat { get; set; }
    public Message Message { get; set; }
    public long ChatId { get; set; }
    public string MessageText { get; set; }
}