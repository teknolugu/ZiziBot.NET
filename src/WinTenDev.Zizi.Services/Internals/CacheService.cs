using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheTower;
using EasyCaching.Core;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Services.Internals;

public class CacheService
{
    private readonly CacheConfig _cacheConfig;
    private readonly IEasyCachingProvider _cachingProvider;
    private readonly ILogger<CacheService> _logger;
    private readonly CacheStack _cacheStack;
    private readonly CacheSettings _cacheSettings;
    private readonly int _expireAfter;
    private readonly int _staleAfter;

    public CacheService(
        IOptionsSnapshot<CacheConfig> cachingConfig,
        IEasyCachingProvider cachingProvider,
        ILogger<CacheService> logger,
        CacheStack cacheStack
    )
    {
        _cacheConfig = cachingConfig.Value;
        _cachingProvider = cachingProvider;
        _logger = logger;
        _cacheStack = cacheStack;

        (_expireAfter, _staleAfter) = cachingConfig.Value;
        _cacheSettings = new CacheSettings(TimeSpan.FromMinutes(_expireAfter), TimeSpan.FromMinutes(_staleAfter));
    }

    /// <summary>
    /// Invalidate all cache based on KnownPrefixes
    /// </summary>
    public void InvalidateAll()
    {
        var knownPrefixes = _cacheConfig.KnownPrefixes;

        if (!_cacheConfig.InvalidateOnStart)
        {
            _logger.LogInformation("Invalidate Cache on start is disabled");
            return;
        }

        _logger.LogInformation("Invalidating caches..");
        knownPrefixes.ForEach(Action);

        void Action(string cachePrefix)
        {
            _cachingProvider.RemoveByPrefix(cachePrefix);
        }

        _logger.LogInformation("Invalidate caches finish");
    }

    /// <summary>
    /// Get all cache keys based on KnownPrefixes
    /// </summary>
    public List<string> GetAllKeys()
    {
        var allKeys = new List<string>();
        var knownPrefixes = _cacheConfig.KnownPrefixes;
        knownPrefixes.ForEach((cachePrefix, _) => {
            var cacheValues = _cachingProvider.GetByPrefix<dynamic>(cachePrefix);

            allKeys.AddRange(cacheValues.Select((dictionary, _) => dictionary.Key));
        });

        return allKeys;
    }

    /// <summary>
    /// Get data, and caching if not exist
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <param name="action"></param>
    /// <typeparam></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T> GetOrSetAsync<T>(
        string cacheKey,
        Func<Task<T>> action
    )
    {
        var cache = await _cacheStack.GetOrSetAsync<T>(cacheKey, async (_) => await action(), _cacheSettings);

        return cache;
    }

    public async Task<T> SetAsync<T>(
        string cacheKey,
        Func<Task<T>> action
    )
    {
        var data = await action();
        var cache = await _cacheStack.SetAsync(cacheKey, data, TimeSpan.FromMinutes(_expireAfter));

        return cache.Value;
    }

    public async Task<T> SetAsync<T>(
        string cacheKey,
        T data
    )
    {
        var cache = await _cacheStack.SetAsync(cacheKey, data, TimeSpan.FromMinutes(_expireAfter));

        return cache.Value;
    }

    public async Task EvictAsync(string cacheKey)
    {
        await _cacheStack.EvictAsync(cacheKey);
    }
}