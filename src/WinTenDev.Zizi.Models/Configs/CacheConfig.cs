using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Configs;

public class CacheConfig
{
    public CacheStorage CacheStorage { get; set; }
    public bool InvalidateOnStart { get; set; }
    public string ExpireAfter { get; set; } = "60m";
    public string StaleAfter { get; set; } = "5s";
    public string[] KnownPrefixes { get; set; }

    public void Deconstruct(
        out string expireAfter,
        out string staleAfter
    )
    {
        expireAfter = ExpireAfter;
        staleAfter = StaleAfter;
    }
}
