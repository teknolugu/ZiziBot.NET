using CsvHelper.Configuration.Attributes;

namespace WinTenDev.Zizi.Models.Types;

public class CommonGlobalBanItem
{
    [Name("id")]
    public long UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Reason { get; set; }
}