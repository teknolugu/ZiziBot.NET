using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Configs;

/// <summary>
/// Property of Caching Configuration.
/// </summary>
public class CacheConfig
{
    /// <summary>
    /// Type of Caching Storage.
    /// </summary>
    public CacheStorage CacheStorage { get; set; }

    /// <summary>
    /// If True, cache will be invalidated on start
    /// </summary>
    public bool InvalidateOnStart { get; set; }

    /// <summary>
    /// Common cache expiration time
    /// </summary>
    public int ExpireAfter { get; set; } = 60;

    /// <summary>
    /// Gets or sets the value of the stale after
    /// </summary>
    public int StaleAfter { get; set; } = 5;

    /// <summary>
    /// Common known cache prefix, will be invalidated on start.
    /// </summary>
    public string[] KnownPrefixes { get; set; }

    /// <summary>
    /// Deconstructs the expire after
    /// </summary>
    /// <param name="expireAfter">The expire after</param>
    /// <param name="staleAfter">The stale after</param>
    public void Deconstruct(out int expireAfter, out int staleAfter)
    {
        expireAfter = ExpireAfter;
        staleAfter = StaleAfter;
    }
}