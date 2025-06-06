using System;
using System.Collections.Generic;
using System.Linq;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// Represents a simple in-memory cache that retains a timestamp for every entry.
/// </summary>
/// <typeparam name="TKey">The type of keys used to identify cached entries.</typeparam>
/// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
/// <remarks>
/// Each call to <see cref="Set(TKey,TValue)"/> or
/// <see cref="SetWithTimestamp(TKey,TValue,System.DateTimeOffset)"/> stores the
/// provided value together with a timestamp. Consumers can later retrieve both
/// pieces of information via <see cref="TryGetWithTimestamp"/>.  The class is
/// fully thread&#8209;safe and raises the <see cref="OnSet"/> event whenever an
/// entry is added or updated, making it suitable for use with the hybrid cache
/// implementations.
/// </remarks>
public class TimestampedMemoryCache<TKey, TValue> :
    ICacheTimestamped<TKey, TValue>,
    ICacheAvailability,
    ICacheEnumerable<TKey>,
    ICacheEvents<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Internal storage backing the cache. Each key maps to a value and the
    /// timestamp representing when it was stored.
    /// </summary>
    private readonly Dictionary<TKey, (TValue Value, DateTimeOffset Timestamp)> _cache = new();

    /// <summary>
    /// Synchronization object used to guard access to <see cref="_cache"/> and
    /// ensure thread&#8209;safe operations.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Logger used for diagnostic output. Defaults to <see cref="NullLogger"/>.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Event raised whenever a value is added or updated in the cache.
    /// </summary>
    /// <remarks>
    /// Hybrid caches subscribe to this event to perform migrations or other
    /// actions when a cached value changes.
    /// </remarks>
    public event Action<TKey, TValue> OnSet = delegate { };

    /// <summary>
    /// Initializes a new instance of the <see cref="TimestampedMemoryCache{TKey,TValue}"/> class
    /// using a <see cref="NullLogger"/> for logging.
    /// </summary>
    public TimestampedMemoryCache() : this(NullLogger.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the cache with the specified <paramref name="logger"/>.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic output.</param>
    public TimestampedMemoryCache(ILogger logger)
    {
        _logger = logger ?? NullLogger.Instance;
        _logger.LogDebug("TimestampedMemoryCache initialized");
    }

    /// <inheritdoc />
    public bool TryGet(TKey key, out TValue value)
        => TryGetWithTimestamp(key, out value, out _);

    /// <summary>
    /// Attempts to retrieve the value and its timestamp for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key whose value should be retrieved.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with <paramref name="key"/>, if found.
    /// </param>
    /// <param name="timestamp">
    /// When this method returns, contains the timestamp when the value was stored, if found.
    /// </param>
    /// <returns><see langword="true"/> if the key exists; otherwise, <see langword="false"/>.</returns>
    public bool TryGetWithTimestamp(TKey key, out TValue value, out DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                value = entry.Value;
                timestamp = entry.Timestamp;
                return true;
            }
        }

        value = default!;
        timestamp = default;
        return false;
    }

    /// <inheritdoc />
    public void Set(TKey key, TValue value)
        => SetWithTimestamp(key, value, DateTimeOffset.UtcNow);

    /// <summary>
    /// Stores the specified <paramref name="value"/> in the cache with the given
    /// <paramref name="key"/> and <paramref name="timestamp"/>.
    /// </summary>
    /// <param name="key">The key to store.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="timestamp">The timestamp associated with the value.</param>
    public void SetWithTimestamp(TKey key, TValue value, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_lock)
        {
            _cache[key] = (value, timestamp);
        }
        OnSet(key, value);
    }

    /// <summary>
    /// Removes the entry associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <returns><see langword="true"/> if the item was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_lock)
        {
            return _cache.Remove(key);
        }
    }

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Returns a snapshot of all keys currently stored in the cache.
    /// </summary>
    public IEnumerable<TKey> Keys()
    {
        lock (_lock)
        {
            return _cache.Keys.ToList();
        }
    }

    /// <summary>
    /// Attempts to perform a round-trip add/remove operation to verify that the
    /// cache is usable in the current environment.
    /// </summary>
    /// <returns><see langword="true"/> if the cache can successfully store data; otherwise <see langword="false"/>.</returns>
    public bool IsAvailable()
    {
        try
        {
            TKey testKey = GenerateTestKey();
            lock (_lock)
            {
                _cache[testKey] = (default!, DateTimeOffset.UtcNow);
                bool result = _cache.ContainsKey(testKey);
                _cache.Remove(testKey);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache availability");
            return false;
        }
    }

    /// <summary>
    /// Creates a temporary key used for availability checks. The method tries to
    /// provide sensible defaults for common primitive types and falls back to
    /// <see cref="Activator.CreateInstance{T}()"/> when possible.
    /// </summary>
    private static TKey GenerateTestKey()
    {
        if (typeof(TKey) == typeof(string))
            return (TKey)(object)Guid.NewGuid().ToString();
        if (typeof(TKey) == typeof(int))
            return (TKey)(object)Random.Shared.Next();
        if (typeof(TKey) == typeof(Guid))
            return (TKey)(object)Guid.NewGuid();
        if (typeof(TKey) == typeof(long))
            return (TKey)(object)Random.Shared.NextInt64();
        try
        {
            return Activator.CreateInstance<TKey>();
        }
        catch
        {
            return default!;
        }
    }
}
