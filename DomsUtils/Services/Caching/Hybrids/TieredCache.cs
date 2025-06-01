using DomsUtils.Services.Caching.Addons.MigrationRules;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Hybrids;

/// <summary>
/// Represents a tiered caching solution designed to work with multiple cache levels.
/// Provides functionality for storing, retrieving, and migrating cache entries between tiers.
/// </summary>
/// <typeparam name="TKey">The type of cache entry key.</typeparam>
/// <typeparam name="TValue">The type of cache entry value.</typeparam>
public class TieredCache<TKey, TValue> : ICache<TKey, TValue>, IDisposable
{
    /// <summary>
    /// Represents the collection of caches used in the tiered caching system.
    /// Contains multiple cache tiers, implemented as a list of <see cref="ICache{TKey, TValue}"/> instances.
    /// Caches are ordered by their tier level, playing a role in retrieval and migration strategies.
    /// </summary>
    private readonly List<ICache<TKey, TValue>> _caches;

    /// <summary>
    /// Represents the set of rules defining migration behavior for cache entries between tiers in a tiered caching system.
    /// </summary>
    /// <remarks>
    /// The migration rules specify conditions under which entries should be moved between different tiers of the cache.
    /// This can include event-driven migrations or periodic migrations, depending on the associated configuration.
    /// </remarks>
    private readonly MigrationRuleSet<TKey, TValue> _migrationRuleSet;

    /// <summary>
    /// A timer used to periodically trigger cache migration based on the rules provided in
    /// <see cref="MigrationRuleSet{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// If the <see cref="MigrationRuleSet{TKey,TValue}.PeriodicCheckInterval"/> is set, this timer
    /// is initialized to invoke <see cref="TieredCache{TKey, TValue}.CheckAndMigrateAll"/>
    /// at the specified interval.
    /// The timer is disposed when the instance of <see cref="TieredCache{TKey, TValue}"/> is disposed.
    /// </remarks>
    private readonly Timer _timer;

    /// <summary>
    /// Represents a tiered cache system composed of multiple cache levels with migration functionality.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used to identify cache entries.</typeparam>
    /// <typeparam name="TValue">The type of the value stored in the cache.</typeparam>
    public TieredCache(MigrationRuleSet<TKey, TValue> migrationRuleSet, params ICache<TKey, TValue>[] caches)
    {
        if (caches == null || caches.Length < 2)
            throw new ArgumentException("At least two caches are required.");

        _caches = new List<ICache<TKey, TValue>>(caches);
        _migrationRuleSet = migrationRuleSet;

        // Subscribe to OnSet events for event-based triggers
        for (int i = 0; i < _caches.Count; i++)
        {
            if (_caches[i] is ICacheEvents<TKey, TValue> evt)
            {
                int tierIndex = i;
                evt.OnSet += (key, value) => HandleEventTriggeredMigration(key, value, tierIndex);
            }
        }

        // If a periodic interval is set, start a timer
        if (_migrationRuleSet?.PeriodicCheckInterval != null)
        {
            var interval = _migrationRuleSet.PeriodicCheckInterval.Value;
            _timer = new Timer(_ => CheckAndMigrateAll(), null, interval, interval);
        }
    }

    /// <summary>
    /// Attempts to retrieve a value associated with the specified key from the tiered cache.
    /// If the key is found in a lower-priority cache, the key-value pair may be promoted to a higher-priority cache.
    /// </summary>
    /// <param name="key">The key of the item to retrieve.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key exists; otherwise, the default value for the type of the <typeparamref name="TValue"/> parameter.</param>
    /// <returns>True if the key was found in any cache; otherwise, false.</returns>
    public bool TryGet(TKey key, out TValue value)
    {
        for (int i = 0; i < _caches.Count; i++)
        {
            var cache = _caches[i];
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
                continue;

            try
            {
                if (cache.TryGet(key, out value))
                {
                    Promote(key, value, i);
                    return true;
                }
            }
            catch
            {
                // Skip failing tier
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Adds or updates a key-value pair in all underlying caches in the tiered cache.
    /// The operation tries to execute on each cache in the tier starting from the highest priority.
    /// </summary>
    /// <param name="key">The key of the item to add or update in the caches.</param>
    /// <param name="value">The value to associate with the key in the caches.</param>
    public void Set(TKey key, TValue value)
    {
        for (int i = 0; i < _caches.Count; i++)
        {
            var cache = _caches[i];
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
                continue;
            try
            {
                cache.Set(key, value);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Removes the specified key from all caches in the tiered cache system.
    /// </summary>
    /// <param name="key">The key to remove from the cache.</param>
    /// <returns>
    /// True if the key was removed from one or more caches; otherwise, false.
    /// </returns>
    public bool Remove(TKey key)
    {
        bool removed = false;
        foreach (var cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
                continue;
            try
            {
                removed |= cache.Remove(key);
            }
            catch
            {
            }
        }

        return removed;
    }

    /// <summary>
    /// Clears all entries in each cache tier, skipping unavailable caches.
    /// Exceptions thrown by individual cache clears are caught and ignored.
    /// </summary>
    public void Clear()
    {
        foreach (var cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
                continue;
            try
            {
                cache.Clear();
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Manually triggers migration for the specified key across cache tiers from the lowest to the highest tier.
    /// </summary>
    /// <param name="key">The key for which the migration process should be triggered.</param>
    public void CheckAndMigrate(TKey key)
    {
        TryGet(key, out _);
    }

    /// <summary>
    /// Iterates through all tiers of the cache, from the lowest to the highest priority,
    /// and evaluates each key for migration based on the defined migration rules.
    /// Only applicable when the underlying caches implement the ICacheEnumerable interface.
    /// </summary>
    public void CheckAndMigrateAll()
    {
        for (int i = _caches.Count - 1; i >= 0; i--)
        {
            if (_caches[i] is ICacheEnumerable<TKey> enumerable)
            {
                foreach (var key in enumerable.Keys())
                {
                    TryGet(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// Handles event-triggered migration by checking migration rules for the given cache tier
    /// and moving the specified key-value pair to a target tier if conditions are met.
    /// </summary>
    /// <param name="key">The key associated with the value to be migrated.</param>
    /// <param name="value">The value related to the specified key for migration evaluation.</param>
    /// <param name="tierIndex">The index of the cache tier where the event was triggered.</param>
    private void HandleEventTriggeredMigration(TKey key, TValue value, int tierIndex)
    {
        // When a new value is set in tierIndex, check rules for demotion or promotion
        foreach (var rule in _migrationRuleSet?.GetRulesForTier(tierIndex) ??
                             Enumerable.Empty<MigrationRule<TKey, TValue>>())
        {
            var fromCache = _caches[tierIndex];
            int targetTier = rule.ToTier;
            var toCache = targetTier >= 0 && targetTier < _caches.Count ? _caches[targetTier] : null;
            if (rule.FromTier == tierIndex && rule.Condition(key, value, fromCache, toCache))
            {
                if (targetTier >= 0 && targetTier < _caches.Count)
                {
                    var targetCache = _caches[targetTier];
                    if (targetCache is ICacheAvailability avail && !avail.IsAvailable())
                        continue;
                    try
                    {
                        // Copy to target tier, then remove from source tier if demotion
                        targetCache.Set(key, value);
                        if (rule.IsDemotion)
                            _caches[tierIndex].Remove(key);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }

    /// <summary>
    /// Promotes a specified key and value from a lower-priority cache to higher-priority caches based on migration rules.
    /// </summary>
    /// <param name="key">The key associated with the value to be promoted.</param>
    /// <param name="value">The value to be promoted to higher-priority caches.</param>
    /// <param name="foundIndex">The index of the cache where the key-value pair was found.</param>
    private void Promote(TKey key, TValue value, int foundIndex)
    {
        for (int j = 0; j < foundIndex; j++)
        {
            var targetCache = _caches[j];
            if (targetCache is ICacheAvailability avail && !avail.IsAvailable())
                continue;
            bool migrate = _migrationRuleSet == null || _migrationRuleSet.ShouldMigrate(key, value, foundIndex, j, _caches[j], targetCache);
            if (migrate)
            {
                try
                {
                    targetCache.Set(key, value);
                }
                catch
                {
                }
            }
        }
    }

    /// <summary>
    /// Releases any resources allocated by the TieredCache, such as timers used for periodic migration checks.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Flag to track whether Dispose has been called.
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// Releases the unmanaged resources used by the TieredCache and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            _timer?.Dispose();

            // Dispose any IDisposable caches
            foreach (var cache in _caches)
            {
                if (cache is IDisposable disposableCache)
                {
                    disposableCache.Dispose();
                }
            }
        }

        // Clean up unmanaged resources here (none in this case)

        _disposed = true;
    }

    /// <summary>
    /// Finalizer to ensure resources are cleaned up if Dispose is not called.
    /// </summary>
    ~TieredCache()
    {
        Dispose(false);
    }
}