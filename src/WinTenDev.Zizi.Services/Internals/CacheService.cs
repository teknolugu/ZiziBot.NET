using System;
using System.Threading.Tasks;
using CacheTower;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.Zizi.Services.Internals;

public class CacheService
{
    private readonly CacheConfig _cacheConfig;
    private readonly ILogger<CacheService> _logger;
    private readonly CacheStack _cacheStack;
    private readonly CacheSettings _cacheSettings;
    private readonly int _expireAfter;
    private readonly int _staleAfter;

    public CacheService(
        IOptionsSnapshot<CacheConfig> cachingConfig,
        // IEasyCachingProvider cachingProvider,
        ILogger<CacheService> logger,
        CacheStack cacheStack
    )
    {
        _cacheConfig = cachingConfig.Value;
        _logger = logger;
        _cacheStack = cacheStack;

        (_expireAfter, _staleAfter) = cachingConfig.Value;
        _cacheSettings = new CacheSettings(TimeSpan.FromMinutes(_expireAfter), TimeSpan.FromSeconds(_staleAfter));
    }

    /// <summary>
    /// Get data, and caching if not exist
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <param name="action"></param>
    /// <param name="disableCache">If this true, cache will be bypassed</param>
    /// <param name="evictBefore">If this true, cache will be evicted before GetOrSet</param>
    /// <param name="evictAfter">If this true, cache will be evicted after GetOrSet</param>
    /// <typeparam></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T> GetOrSetAsync<T>(
        string cacheKey,
        Func<Task<T>> action,
        bool disableCache = false,
        bool evictBefore = false,
        bool evictAfter = false
    )
    {
        if (disableCache) return await action();

        if (evictBefore) await EvictAsync(cacheKey);

        var cache = await _cacheStack.GetOrSetAsync<T>(
            cacheKey: cacheKey,
            getter: async (_) => await action(),
            settings: _cacheSettings
        );

        if (evictAfter) await EvictAsync(cacheKey);

        return cache;
    }

    public async Task<T> SetAsync<T>(
        string cacheKey,
        Func<Task<T>> action
    )
    {
        var data = await action();

        var cache = await _cacheStack.SetAsync(
            cacheKey,
            value: data,
            timeToLive: TimeSpan.FromMinutes(_expireAfter)
        );

        return cache.Value;
    }

    public async Task<T> SetAsync<T>(
        string cacheKey,
        T data
    )
    {
        var cache = await _cacheStack.SetAsync(
            cacheKey: cacheKey,
            value: data,
            timeToLive: TimeSpan.FromMinutes(_expireAfter)
        );

        return cache.Value;
    }

    public async Task EvictAsync(string cacheKey)
    {
        _logger.LogDebug("Evicting cache with key: {CacheKey}", cacheKey);
        await _cacheStack.EvictAsync(cacheKey);
    }
}