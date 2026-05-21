using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kavita.API.Services;

public interface IFileCacheService
{
    /// <summary>
    /// Returns a cached value for the given key if it exists and is within TTL, otherwise calls fetch,
    /// caches the result, and returns it.
    /// </summary>
    Task<T?> GetOrFetchAsync<T>(string key, string cacheBucket, TimeSpan ttl,
        Func<CancellationToken, Task<T?>> fetch,
        Func<T?, bool>? shouldCache = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes the cache entry for the given key.
    /// </summary>
    void Invalidate(string key, string cacheBucket);

    /// <summary>
    /// Deletes all cache entries that start with a given key.
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="cacheBucket"></param>
    void InvalidatePrefix(string prefix, string cacheBucket);
}
