using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CacheTower;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WinTenDev.Zizi.Services.Internals;

public class CacheService
{
    private readonly IOptionsSnapshot<CacheConfig> _cacheConfigSnapshot;
    private readonly ILogger<CacheService> _logger;
    private readonly ICacheStack _cacheStack;
    private string _expireAfter;
    private string _staleAfter;

    private CacheConfig CacheConfig => _cacheConfigSnapshot.Value;

    public CacheService(
        IOptionsSnapshot<CacheConfig> cachingConfigSnapshot,
        ILogger<CacheService> logger,
        ICacheStack cacheStack
    )
    {
        _cacheConfigSnapshot = cachingConfigSnapshot;
        _logger = logger;
        _cacheStack = cacheStack;

        (_expireAfter, _staleAfter) = CacheConfig;
    }

    /// <summary>
    /// Get data, and caching if not exist
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <param name="action"></param>
    /// <param name="disableCache">If this true, cache will be bypassed</param>
    /// <param name="evictBefore">If this true, cache will be evicted before GetOrSet</param>
    /// <param name="evictAfter">If this true, cache will be evicted after GetOrSet</param>
    /// <param name="expireAfter">If this parameter given, global config will be overridden</param>
    /// <param name="staleAfter">If this parameter given, global config will be overridden</param>
    /// <typeparam></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T> GetOrSetAsync<T>(
        string cacheKey,
        Func<Task<T>> action,
        bool disableCache = false,
        bool evictBefore = false,
        bool evictAfter = false,
        string expireAfter = null,
        string staleAfter = null
    )
    {
        if (disableCache || !CacheConfig.IsEnableCache)
            return await action();

        if (evictBefore) await EvictAsync(cacheKey);

        if (expireAfter != null) _expireAfter = expireAfter;
        if (staleAfter != null) _staleAfter = staleAfter;

        var expireAfterSpan = _expireAfter.ToTimeSpan();
        var staleAfterSpan = _staleAfter.ToTimeSpan();

        try
        {
            _logger.LogDebug(
                "Loading Cache value with Key: {CacheKey}. StaleAfter: {StaleAfter}. ExpireAfter: {ExpireAfter}",
                cacheKey,
                staleAfterSpan,
                expireAfterSpan
            );

            var cacheSettings = new CacheSettings(expireAfterSpan, staleAfterSpan);

            var cache = await _cacheStack.GetOrSetAsync<T>(
                cacheKey: cacheKey.Trim(),
                valueFactory: async (_) => await action(),
                settings: cacheSettings
            );

            if (evictAfter) await EvictAsync(cacheKey);

            return cache;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Demystify(), "Error loading cache with Key: {Key}",
                cacheKey);

            await EvictAsync(cacheKey);

            var data = await action();

            return data;
        }
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
            timeToLive: _expireAfter.ToTimeSpan()
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
            timeToLive: _expireAfter.ToTimeSpan()
        );

        return cache.Value;
    }

    public async Task EvictAsync(string cacheKey)
    {
        _logger.LogDebug("Evicting cache with key: {CacheKey}", cacheKey);
        await _cacheStack.EvictAsync(cacheKey);
    }
}