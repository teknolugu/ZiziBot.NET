using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Models.Dto;

public class MessageResponseDto
{
    public string MessageText { get; set; }
    public IReplyMarkup ReplyMarkup { get; set; }
    public bool DisableWebPreview { get; set; } = true;
    public long ReplyToMessageId { get; set; }
}