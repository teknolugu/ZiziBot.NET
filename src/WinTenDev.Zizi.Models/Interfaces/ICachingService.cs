using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Models.Interfaces;

/// <summary>
/// Interface of CachingService
/// </summary>
public interface ICachingService
{
    /// <summary>
    /// Invalidate all cache based on KnownPrefixes
    /// </summary>
    public void InvalidateAll();

    /// <summary>
    /// Remove all expired cache entries
    /// </summary>
    /// <summary>
    /// Get all cache keys based on KnownPrefixes
    /// </summary>
    public List<string> GetAllKeys();

    /// <summary>
    /// Get and set cache with Func
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Task<T> GetAndSetAsync<T>(string cacheKey, Func<Task<T>> action);
}