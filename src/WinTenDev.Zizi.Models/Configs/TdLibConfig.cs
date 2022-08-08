using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Configs;

public class TdLibConfig
{
    public bool IsEnabled { get; set; }
    public string ApiId { get; set; }
    public string ApiHash { get; set; }
    public string PhoneNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public WTelegramSessionStore WTelegramSessionStore { get; set; }
}