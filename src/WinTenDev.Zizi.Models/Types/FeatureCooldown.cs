using System;

namespace WinTenDev.Zizi.Models.Types;

public class FeatureCooldown
{
    public string Guid { get; set; }
    public string FeatureName { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public DateTime NextAvailable { get; set; }
    public DateTime LastUsed { get; set; }
}
