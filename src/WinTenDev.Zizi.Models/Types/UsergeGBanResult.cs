using System;

namespace WinTenDev.Zizi.Models.Types;

public class UsergeGBanResult
{
    public bool Success { get; set; }
    public long UserId { get; set; }
    public string Message { get; set; }
    public string Reason { get; set; }
    public DateTime Date { get; set; }
}