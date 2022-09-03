namespace WinTenDev.Zizi.Models.Dto;

public class RssSourceDto
{
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public bool IsEnabled { get; set; }
    public string UrlFeed { get; set; }
}