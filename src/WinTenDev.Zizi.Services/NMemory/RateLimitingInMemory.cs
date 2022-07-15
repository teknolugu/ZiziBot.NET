using NMemory;
using NMemory.Tables;

namespace WinTenDev.Zizi.Services.NMemory;

public class RateLimitingInMemory : Database
{
    public ITable<FeatureCooldown> FeatureCooldowns { get; set; }

    public RateLimitingInMemory()
    {
        FeatureCooldowns = Tables.Create<FeatureCooldown, string>(cooldown => cooldown.Guid);
    }
}