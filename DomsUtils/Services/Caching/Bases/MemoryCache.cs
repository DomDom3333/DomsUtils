using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// Represents a thread-safe in-memory cache with support for events, enumeration, and availability checks.
/// </summary>
/// <typeparam name="TKey">The type of the keys used to identify values in the cache.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the cache.</typeparam>
public class MemoryCache<TKey, TValue> : CacheBase<TKey, TValue>, ICacheAvailability, ICacheEnumerable<TKey>,
    ICacheEvents<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Stores the key-value pairs for the in-memory cache.
    /// </summary>
    /// <remarks>
    /// This dictionary is the core data structure where all cached items are held.
    /// It is internally synchronized using a locking mechanism to provide thread-safe operations on the cache.
    /// </remarks>
    private readonly Dictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();

    /// <summary>
    /// Provides a thread-safety mechanism for accessing or modifying the internal cache.
    /// </summary>
    /// <remarks>
    /// The <c>_lock</c> object is used to synchronize access to critical sections of code
    /// that perform operations on the internal cache. This ensures consistency and prevents
    /// potential race conditions in concurrent environments.
    /// </remarks>
    private readonly Lock _lock = new Lock();

    /// <summary>
    /// Occurs when a key-value pair is added or updated in the cache.
    /// </summary>
    /// <remarks>
    /// The <c>OnSet</c> event provides a mechanism for subscribers to get notified
    /// whenever a key-value pair is set in the cache. This can be useful for scenarios
    /// such as tiered caching, logging, or triggering other actions based on cache updates.
    /// </remarks>
    /// <typeparam name="TKey">The type of the key used in the cache.</typeparam>
    /// <typeparam name="TValue">The type of the value stored in the cache.</typeparam>
    public event Action<TKey, TValue> OnSet;

    /// <summary>
    /// Attempts to retrieve a value from the cache associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the cache entry to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// true if the key was found in the cache; otherwise, false.
    /// </returns>
    public bool TryGet(TKey key, out TValue value) => TryGetInternal(key, out value);

    /// <summary>
    /// Tries to retrieve a value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to retrieve from the cache.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>True if the specified key exists in the cache; otherwise, false.</returns>
    protected override bool TryGetInternal(TKey key, out TValue value)
    {
        lock (_lock)
        {
            return _cache.TryGetValue(key, out value);
        }
    }

    /// <summary>
    /// Adds or updates a key-value pair in the memory cache. If the key already exists, its value is updated;
    /// otherwise, a new key-value pair is added. Triggers the <see cref="OnSet"/> event when a key-value pair is successfully set.
    /// </summary>
    /// <param name="key">The key to add or update in the cache.</param>
    /// <param name="value">The value to associate with the specified key in the cache.</param>
    public override void Set(TKey key, TValue value)
    {
        base.Set(key, value);
        OnSet?.Invoke(key, value);
    }

    /// <summary>
    /// Sets the specified key-value pair in the cache. If the key already exists,
    /// its value will be updated. This operation is thread-safe.
    /// </summary>
    /// <param name="key">The key associated with the value to be stored in the cache.</param>
    /// <param name="value">The value to be stored in the cache for the specified key.</param>
    protected override void SetInternal(TKey key, TValue value)
    {
        lock (_lock)
        {
            _cache[key] = value;
        }
    }

    /// <summary>
    /// Removes an item from the cache corresponding to the specified key.
    /// </summary>
    /// <param name="key">The key of the item to remove from the cache.</param>
    /// <returns>
    /// True if the item was successfully removed from the cache; otherwise, false.
    /// </returns>
    protected override bool RemoveInternal(TKey key)
    {
        lock (_lock)
        {
            return _cache.Remove(key);
        }
    }

    /// <summary>
    /// Clears all entries in the cache.
    /// This method is protected and is meant to be used internally by subclasses
    /// to remove all items from the underlying cache data structure.
    /// </summary>
    protected override void ClearInternal()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Retrieves a collection of all keys currently stored in the cache.
    /// </summary>
    /// <returns>
    /// An enumerable containing all keys in the cache.
    /// </returns>
    public IEnumerable<TKey> Keys()
    {
        lock (_lock)
        {
            return _cache.Keys.ToList();
        }
    }

    /// <summary>
    /// Checks if the memory cache is currently available for usage.
    /// Performs a round-trip add, get, remove operation with a unique key
    /// to ensure the cache is fully operational and thread-safe.
    /// </summary>
    /// <returns>True if the cache is available; otherwise, false.</returns>
    public override bool IsAvailable()
    {
        var testKey = (TKey)(object)Guid.NewGuid().ToString(); // Works for string keys. For other types, provide a suitable unique key.
        var testValue = default(TValue);
        try
        {
            lock (_lock)
            {
                // Try to add and retrieve a dummy value
                _cache[testKey] = testValue;
                var success = _cache.TryGetValue(testKey, out _);
                _cache.Remove(testKey);
                return success;
            }
        }
        catch
        {
            return false;
        }
    }
}