namespace WinTenDev.Zizi.Models.Dto;

public class WordFilterDto
{
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public string Word { get; set; }
    public bool IsGlobal { get; set; }
}