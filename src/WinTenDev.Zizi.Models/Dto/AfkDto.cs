namespace WinTenDev.Zizi.Models.Dto;

public class AfkDto
{
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public string Reason { get; set; }
    public bool IsAfk { get; set; }
}