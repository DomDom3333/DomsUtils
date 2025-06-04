using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Hybrids.ParallelCache;

/// <summary>
/// Represents a hybrid caching mechanism that uses multiple underlying caches to store and retrieve data.
/// Reads are performed in priority order, and writes are executed in parallel across all caches.
/// </summary>
/// <typeparam name="TKey">The type of the keys used for identifying cached entries.</typeparam>
/// <typeparam name="TValue">The type of the values being cached.</typeparam>
public class ParallelCache<TKey, TValue> : ICache<TKey, TValue>, ICacheMigratable, ICacheAvailability
{
    private readonly IList<ICache<TKey, TValue>> _caches;
    private readonly ILogger _logger;
    private readonly SyncOptions _syncOptions;

    /// <summary>
    /// Hybrid cache that writes to all underlying caches in parallel, and reads from them in priority order.
    /// The first cache in the array has the highest priority.
    /// </summary>
    /// <param name="caches">Array of cache implementations, ordered by priority (first = highest priority)</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="syncOptions">Synchronization behavior options</param>
    public ParallelCache(ICache<TKey, TValue>[] caches, ILogger logger = null, SyncOptions syncOptions = null)
    {
        if (caches == null || caches.Length < 2)
            throw new ArgumentException("At least two caches are required for a ParallelCache.");

        _caches = new List<ICache<TKey, TValue>>(caches);
        _logger = logger ?? NullLogger.Instance;
        _syncOptions = syncOptions ?? new SyncOptions();

        _logger.LogInformation("ParallelCache initialized with {CacheCount} caches.", _caches.Count);
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the first available cache in priority order.
    /// </summary>
    public bool TryGet(TKey key, out TValue value)
    {
        _logger.LogDebug("Attempting to retrieve key '{Key}' from ParallelCache.", key);
        
        foreach (ICache<TKey, TValue> cache in _caches)
        {
            if (!IsCacheAvailable(cache))
                continue;

            try
            {
                if (cache.TryGet(key, out value))
                {
                    _logger.LogDebug("Key '{Key}' found in cache.", key);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving key '{Key}' from cache.", key);
            }
        }

        value = default;
        _logger.LogDebug("Key '{Key}' not found in any cache.", key);
        return false;
    }

    /// <summary>
    /// Writes a key-value pair to all available underlying caches in parallel.
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        _logger.LogDebug("Setting key '{Key}' in all caches.", key);
        
        Task[] tasks = _caches
            .Where(IsCacheAvailable)
            .Select(cache => Task.Run(() =>
            {
                try
                {
                    cache.Set(key, value);
                    _logger.LogTrace("Key '{Key}' set in cache.", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting key '{Key}' in cache.", key);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);
    }

    public bool Remove(TKey key)
    {
        _logger.LogDebug("Removing key '{Key}' from all caches.", key);
        
        bool[] removed = new bool[1]; // Use array for reference semantics
        Task[] tasks = _caches
            .Where(IsCacheAvailable)
            .Select(cache => Task.Run(() =>
            {
                try
                {
                    if (cache.Remove(key))
                    {
                        removed[0] = true;
                        _logger.LogTrace("Key '{Key}' removed from cache.", key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing key '{Key}' from cache.", key);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        if (removed[0])
            _logger.LogDebug("Key '{Key}' successfully removed from one or more caches.", key);

        return removed[0];
    }

    /// <summary>
    /// Clears the contents of all underlying caches.
    /// </summary>
    public void Clear()
    {
        _logger.LogWarning("Clearing all caches in ParallelCache.");
        
        Task[] tasks = _caches
            .Where(IsCacheAvailable)
            .Select(cache => Task.Run(() =>
            {
                try
                {
                    cache.Clear();
                    _logger.LogTrace("Cache cleared successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing cache.");
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);
    }

    /// <summary>
    /// Determines whether at least one underlying cache is available for use.
    /// </summary>
    public bool IsAvailable()
    {
        return _caches.Any(IsCacheAvailable);
    }

    /// <summary>
    /// Synchronizes all caches to ensure data consistency.
    /// Uses consensus-based approach: if majority of caches don't have a key, it's considered deleted.
    /// </summary>
    public void TriggerMigrationNow()
    {
        _logger.LogInformation("Starting cache synchronization.");
        
        try
        {
            SynchronizeCaches();
            _logger.LogInformation("Cache synchronization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache synchronization failed.");
            throw;
        }
    }

    private void SynchronizeCaches()
    {
        List<ICache<TKey, TValue>> availableCaches = _caches.Where(IsCacheAvailable).ToList();
        
        if (availableCaches.Count < 2)
        {
            _logger.LogWarning("Not enough available caches for synchronization.");
            return;
        }

        // Collect all keys from all caches
        Dictionary<ICache<TKey, TValue>, HashSet<TKey>> allKeysFromCaches = new Dictionary<ICache<TKey, TValue>, HashSet<TKey>>();
        HashSet<TKey> allUniqueKeys = new HashSet<TKey>();

        foreach (ICache<TKey, TValue> cache in availableCaches)
        {
            if (cache is ICacheEnumerable<TKey> enumerable)
            {
                HashSet<TKey> keys = enumerable.Keys().ToHashSet();
                allKeysFromCaches[cache] = keys;
                foreach (TKey key in keys)
                    allUniqueKeys.Add(key);
            }
            else
            {
                allKeysFromCaches[cache] = new HashSet<TKey>();
            }
        }

        int totalSynced = 0;
        int totalRemoved = 0;

        // Process each unique key
        foreach (TKey key in allUniqueKeys)
        {
            List<ICache<TKey, TValue>> cachesWithKey = availableCaches.Where(c => allKeysFromCaches[c].Contains(key)).ToList();
            List<ICache<TKey, TValue>> cachesWithoutKey = availableCaches.Where(c => !allKeysFromCaches[c].Contains(key)).ToList();

            // Apply consensus logic
            if (ShouldRemoveKey(key, cachesWithoutKey.Count, availableCaches.Count))
            {
                // Remove key from caches that have it
                foreach (ICache<TKey, TValue> cache in cachesWithKey)
                {
                    try
                    {
                        cache.Remove(key);
                        totalRemoved++;
                        _logger.LogTrace("Removed key '{Key}' during sync (consensus: deleted).", key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error removing key '{Key}' during sync.", key);
                    }
                }
            }
            else if (cachesWithKey.Count > 0 && cachesWithoutKey.Count > 0)
            {
                // Get value from first available cache that has it
                ICache<TKey, TValue> sourceCache = cachesWithKey.First();
                if (sourceCache.TryGet(key, out TValue? value))
                {
                    // Sync to caches that don't have it
                    foreach (ICache<TKey, TValue> cache in cachesWithoutKey)
                    {
                        try
                        {
                            cache.Set(key, value);
                            totalSynced++;
                            _logger.LogTrace("Synced key '{Key}' during sync.", key);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error syncing key '{Key}' during sync.", key);
                        }
                    }
                }
            }
        }

        _logger.LogInformation("Sync completed: {Synced} keys synced, {Removed} keys removed.", totalSynced, totalRemoved);
    }

    private bool ShouldRemoveKey(TKey key, int cachesWithoutKey, int totalCaches)
    {
        return _syncOptions.ConflictResolution switch
        {
            SyncConflictResolution.MajorityWins => cachesWithoutKey > (totalCaches * _syncOptions.MajorityThreshold),
            SyncConflictResolution.PrimaryWins => !GetKeysIfSupported(_caches.First()).Contains(key),
            _ => false
        };
    }

    // Add this helper method to your ParallelCache class:
    private IEnumerable<TKey> GetKeysIfSupported(ICache<TKey, TValue> cache)
    {
        // Ensure your "using" statements reference the correct namespace for ICacheEnumerable<TKey>
        if (cache is DomsUtils.Services.Caching.Interfaces.Addons.ICacheEnumerable<TKey> enumerableCache)
        {
            return enumerableCache.Keys();
        }

        return Enumerable.Empty<TKey>();
    }

    private bool IsCacheAvailable(ICache<TKey, TValue> cache)
    {
        return cache is not ICacheAvailability availability || availability.IsAvailable();
    }
}