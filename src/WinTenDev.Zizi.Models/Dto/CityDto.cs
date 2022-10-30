namespace WinTenDev.Zizi.Models.Dto;

public class CityDto
{
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public long CityId { get; set; }
    public string CityName { get; set; }
    public bool EnableNotification { get; set; }
}