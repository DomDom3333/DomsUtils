using System;
using System.Collections.Generic;
using System.Threading;
using DomsUtils.Services.Caching.Bases;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Hybrids;

/// <summary>
/// Hybrid cache that combines a <see cref="TimestampedMemoryCache{TKey,TValue}"/>
/// with a persistent cache. Entries older than the configured age are migrated
/// from memory to the persistent cache.
/// </summary>
/// <typeparam name="TKey">Cache key type.</typeparam>
/// <typeparam name="TValue">Cache value type.</typeparam>
public sealed class TimeBasedHybridCache<TKey, TValue> :
    ICache<TKey, TValue>, ICacheMigratable, IDisposable
    where TKey : notnull
{
    private readonly TimestampedMemoryCache<TKey, TValue> _memoryCache;
    private readonly ICache<TKey, TValue> _persistentCache;
    private readonly TimeSpan _demotionAge;
    private readonly Timer _timer;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeBasedHybridCache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="memoryCache">The in-memory cache used for fast access.</param>
    /// <param name="persistentCache">The persistent cache for long-term storage.</param>
    /// <param name="demotionAge">Entries older than this age will be demoted to the persistent cache.</param>
    /// <param name="checkInterval">Interval for checking and migrating old entries.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public TimeBasedHybridCache(
        TimestampedMemoryCache<TKey, TValue> memoryCache,
        ICache<TKey, TValue> persistentCache,
        TimeSpan demotionAge,
        TimeSpan checkInterval,
        ILogger? logger = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _persistentCache = persistentCache ?? throw new ArgumentNullException(nameof(persistentCache));
        _demotionAge = demotionAge;
        _logger = logger ?? NullLogger.Instance;
        _timer = new Timer(_ => DemoteOldEntries(), null, checkInterval, checkInterval);
    }

    /// <inheritdoc />
    public bool TryGet(TKey key, out TValue value)
    {
        if (_memoryCache.TryGet(key, out value))
            return true;

        if (_persistentCache.TryGet(key, out value))
        {
            _memoryCache.Set(key, value);
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public void Set(TKey key, TValue value)
    {
        _memoryCache.Set(key, value);
        _persistentCache.Set(key, value);
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        bool removedMemory = _memoryCache.Remove(key);
        bool removedPersistent = _persistentCache.Remove(key);
        return removedMemory || removedPersistent;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _memoryCache.Clear();
        _persistentCache.Clear();
    }

    /// <inheritdoc />
    public void TriggerMigrationNow()
    {
        _logger.LogDebug("Manual migration triggered for TimeBasedHybridCache.");
        DemoteOldEntries();
    }

    private void DemoteOldEntries()
    {
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (TKey key in _memoryCache.Keys())
            {
                if (_memoryCache.TryGetWithTimestamp(key, out TValue value, out DateTimeOffset ts))
                {
                    if (now - ts >= _demotionAge)
                    {
                        _persistentCache.Set(key, value);
                        _memoryCache.Remove(key);
                        _logger.LogDebug("Demoted key {Key} to persistent cache.", key);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demoting entries in TimeBasedHybridCache.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer.Dispose();
        if (_persistentCache is IDisposable d)
            d.Dispose();
    }
}
