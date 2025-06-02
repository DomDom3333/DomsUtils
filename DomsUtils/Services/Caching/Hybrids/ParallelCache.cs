using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Hybrids;

/// <summary>
/// Represents a hybrid caching mechanism that uses multiple underlying caches to store and retrieve data.
/// Reads are performed in priority order, and writes are executed in parallel across all caches.
/// </summary>
/// <typeparam name="TKey">The type of the keys used for identifying cached entries.</typeparam>
/// <typeparam name="TValue">The type of the values being cached.</typeparam>
public class ParallelCache<TKey, TValue> : ICache<TKey, TValue>, ICacheAvailability
{
    /// <summary>
    /// A collection of underlying cache implementations that the ParallelCache works with.
    /// Used to manage multiple cache instances for read/write and availability operations.
    /// </summary>
    private readonly IList<ICache<TKey, TValue>> _caches;

    private readonly ILogger _logger;

    /// <summary>
    /// Hybrid cache that writes to all underlying caches in parallel, and reads from them in priority order.
    /// Implements ICache so it can be nested.
    /// </summary>
    public ParallelCache(params ICache<TKey, TValue>[] caches)
    {
        if (caches == null || caches.Length < 2)
            throw new ArgumentException("At least two caches are required for a ParallelCache.");

        _caches = new List<ICache<TKey, TValue>>(caches);
        _logger = NullLogger.Instance;

        _logger.LogInformation("ParallelCache initialized with {CacheCount} caches.", _caches.Count);
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the first available cache in order.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key if found, or the default value for the type if not found.</param>
    /// <returns>True if the value is found in any cache; otherwise, false.</returns>
    public bool TryGet(TKey key, out TValue value)
    {
        _logger.LogDebug("Attempting to retrieve key '{Key}' from ParallelCache.", key);
        foreach (ICache<TKey, TValue> cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache is unavailable for retrieving key '{Key}'.", key);
                continue;
            }

            try
            {
                if (cache.TryGet(key, out value))
                {
                    _logger.LogDebug("Key '{Key}' found in a cache.", key);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving key '{Key}' from a cache.", key);
            }
        }

        value = default;
        _logger.LogDebug("Key '{Key}' not found in any cache.", key);
        return false;
    }

    /// <summary>
    /// Writes a key-value pair to all available underlying caches in parallel.
    /// </summary>
    /// <param name="key">The key associated with the value to be cached.</param>
    /// <param name="value">The value to be cached.</param>
    public void Set(TKey key, TValue value)
    {
        _logger.LogDebug("Setting key '{Key}' in all caches.", key);
        foreach (ICache<TKey, TValue> cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache is unavailable for setting key '{Key}'.", key);
                continue;
            }

            try
            {
                cache.Set(key, value);
                _logger.LogDebug("Key '{Key}' set in a cache.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting key '{Key}' in a cache.", key);
            }
        }
    }

    /// <summary>
    /// Removes the specified key from all available caches.
    /// </summary>
    /// <param name="key">The key to remove from the caches.</param>
    /// <returns>
    /// A boolean value indicating whether the key was successfully removed from at least one cache.
    /// </returns>
    public bool Remove(TKey key)
    {
        _logger.LogDebug("Removing key '{Key}' from all caches.", key);
        bool removed = false;
        foreach (ICache<TKey, TValue> cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache is unavailable for removing key '{Key}'.", key);
                continue;
            }

            try
            {
                removed |= cache.Remove(key);
                _logger.LogDebug("Key '{Key}' removed from a cache.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing key '{Key}' from a cache.", key);
            }
        }

        if (removed)
            _logger.LogInformation("Key '{Key}' successfully removed from one or more caches.", key);
        else
            _logger.LogWarning("Key '{Key}' not found in any cache for removal.", key);

        return removed;
    }

    /// <summary>
    /// Clears the contents of all underlying caches that are currently available.
    /// </summary>
    public void Clear()
    {
        _logger.LogWarning("Clearing all caches in ParallelCache.");
        foreach (ICache<TKey, TValue> cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache is unavailable for clearing.");
                continue;
            }

            try
            {
                cache.Clear();
                _logger.LogInformation("Cache cleared successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing a cache.");
            }
        }
    }

    /// <summary>
    /// Determines whether at least one underlying cache is available for use.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the cache system is operational and available.
    /// Returns true if any underlying cache is available, otherwise false.
    /// </returns>
    public bool IsAvailable()
    {
        _logger.LogDebug("Checking availability of ParallelCache.");
        bool available = _caches.Any(c => !(c is ICacheAvailability avail) || avail.IsAvailable());

        if (available)
            _logger.LogDebug("ParallelCache is available.");
        else
            _logger.LogWarning("ParallelCache is not available.");

        return available;
    }
}
