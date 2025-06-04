using DomsUtils.Services.Caching.Addons.MigrationRules;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Hybrids;

/// <summary>
/// Represents a tiered caching solution designed to work with multiple cache levels.
/// Provides functionality for storing, retrieving, and migrating cache entries between tiers.
/// </summary>
/// <typeparam name="TKey">The type of cache entry key.</typeparam>
/// <typeparam name="TValue">The type of cache entry value.</typeparam>
public class TieredCache<TKey, TValue> : ICache<TKey, TValue>, ICacheMigratable, IDisposable
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
    /// Logger instance for recording diagnostic information, warnings, and errors.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Represents a tiered cache system composed of multiple cache levels with migration functionality.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used to identify cache entries.</typeparam>
    /// <typeparam name="TValue">The type of the value stored in the cache.</typeparam>
    public TieredCache(MigrationRuleSet<TKey, TValue> migrationRuleSet, ILogger logger = default, params ICache<TKey, TValue>[] caches)
    {
        if (caches == null || caches.Length < 2)
            throw new ArgumentException("At least two caches are required.");

        _caches = new List<ICache<TKey, TValue>>(caches);
        _migrationRuleSet = migrationRuleSet;
        _logger = logger ?? NullLogger.Instance;

        _logger.LogInformation("TieredCache initialized with {CacheCount} caches.", _caches.Count);

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
            TimeSpan interval = _migrationRuleSet.PeriodicCheckInterval.Value;
            _timer = new Timer(_ => CheckAndMigrateAll(), null, interval, interval);
            _logger.LogInformation("Periodic migration timer started with interval {Interval}.", interval);
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
        _logger.LogDebug("Attempting to retrieve key '{Key}' from TieredCache.", key);
        for (int i = 0; i < _caches.Count; i++)
        {
            ICache<TKey, TValue> cache = _caches[i];
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache tier {TierIndex} is unavailable.", i);
                continue;
            }

            try
            {
                if (cache.TryGet(key, out value))
                {
                    _logger.LogDebug("Key '{Key}' found in tier {TierIndex}. Promoting.", key, i);
                    Promote(key, value, i);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving key '{Key}' from tier {TierIndex}.", key, i);
            }
        }

        value = default;
        _logger.LogDebug("Key '{Key}' not found in any tier.", key);
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
        _logger.LogDebug("Setting key '{Key}' in all tiers.", key);
        for (int i = 0; i < _caches.Count; i++)
        {
            ICache<TKey, TValue> cache = _caches[i];
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache tier {TierIndex} is unavailable for setting key '{Key}'.", i, key);
                continue;
            }

            try
            {
                cache.Set(key, value);
                _logger.LogDebug("Key '{Key}' set in tier {TierIndex}.", key, i);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting key '{Key}' in tier {TierIndex}.", key, i);
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
        _logger.LogDebug("Removing key '{Key}' from all tiers.", key);
        bool removed = false;
        foreach (ICache<TKey, TValue> cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache tier is unavailable for removing key '{Key}'.", key);
                continue;
            }

            try
            {
                removed |= cache.Remove(key);
                _logger.LogDebug("Key '{Key}' removed from a tier.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing key '{Key}' from a tier.", key);
            }
        }

        if (removed)
            _logger.LogInformation("Key '{Key}' successfully removed from one or more tiers.", key);
        else
            _logger.LogWarning("Key '{Key}' not found in any tier for removal.", key);

        return removed;
    }

    /// <summary>
    /// Clears all entries in each cache tier, skipping unavailable caches.
    /// Exceptions thrown by individual cache clears are caught and ignored.
    /// </summary>
    public void Clear()
    {
        _logger.LogWarning("Clearing all tiers in TieredCache.");
        foreach (ICache<TKey, TValue> cache in _caches)
        {
            if (cache is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Cache tier is unavailable for clearing.");
                continue;
            }

            try
            {
                cache.Clear();
                _logger.LogInformation("Cache tier cleared successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing a cache tier.");
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
    private void CheckAndMigrateAll()
    {
        for (int i = _caches.Count - 1; i >= 0; i--)
        {
            if (_caches[i] is ICacheEnumerable<TKey> enumerable)
            {
                foreach (TKey key in enumerable.Keys())
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
        _logger.LogDebug("Handling event-triggered migration for key '{Key}' in tier {TierIndex}.", key, tierIndex);
        // When a new value is set in tierIndex, check rules for demotion or promotion
        foreach (MigrationRule<TKey, TValue> rule in _migrationRuleSet?.GetRulesForTier(tierIndex) ??
                                                     Enumerable.Empty<MigrationRule<TKey, TValue>>())
        {
            ICache<TKey, TValue> fromCache = _caches[tierIndex];
            int targetTier = rule.ToTier;
            ICache<TKey, TValue>? toCache = targetTier >= 0 && targetTier < _caches.Count ? _caches[targetTier] : null;
            if (rule.FromTier == tierIndex && rule.Condition(key, value, fromCache, toCache))
            {
                if (targetTier >= 0 && targetTier < _caches.Count)
                {
                    ICache<TKey, TValue> targetCache = _caches[targetTier];
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

    private void Promote(TKey key, TValue value, int foundIndex)
    {
        // Promote to all higher-priority tiers (lower indices)
        for (int targetTierIndex = 0; targetTierIndex < foundIndex; targetTierIndex++)
        {
            try
            {
                var targetCache = _caches[targetTierIndex];
                var sourceCache = _caches[foundIndex];
            
                // Check if this cache is available
                if (targetCache is ICacheAvailability aval && !aval.IsAvailable())
                {
                    _logger?.LogWarning("Cache tier {TierIndex} is not available for promotion", targetTierIndex);
                    continue;
                }

                // Check migration rules before promoting
                if (_migrationRuleSet != null && 
                    !_migrationRuleSet.ShouldMigrate(key, value, foundIndex, targetTierIndex, sourceCache, targetCache))
                {
                    _logger?.LogDebug("Migration rule prevented promotion of key {Key} from tier {SourceTier} to tier {TargetTier}", 
                        key, foundIndex, targetTierIndex);
                    continue;
                }

                targetCache.Set(key, value);
                _logger?.LogDebug("Promoted key {Key} from tier {SourceTier} to tier {TargetTier}", 
                    key, foundIndex, targetTierIndex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error promoting key {Key} to tier {TierIndex}", key, targetTierIndex);
            }
        }
    }

    /// <summary>
    /// Releases any resources allocated by the TieredCache, such as timers used for periodic migration checks.
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("Disposing TieredCache.");
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
            foreach (ICache<TKey, TValue> cache in _caches)
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

    public void TriggerMigrationNow()
    {
        _logger.LogInformation("Manual migration trigger invoked for TieredCache.");
        CheckAndMigrateAll();
    }
}