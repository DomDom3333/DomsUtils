using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// Represents an in-memory cache implementation that uses key-value pairs for storing data.
/// </summary>
/// <typeparam name="TKey">The type of the cache entry key.</typeparam>
/// <typeparam name="TValue">The type of the cache entry value.</typeparam>
/// <remarks>
/// This class provides thread-safe caching operations and supports events for cache manipulation.
/// It inherits from <see cref="CacheBase{TKey, TValue}"/> and implements the following interfaces:
/// <list type="bullet">
/// <item><see cref="ICacheAvailability"/></item>
/// <item><see cref="ICacheEnumerable{TKey}"/></item>
/// <item><see cref="ICacheEvents{TKey, TValue}"/></item>
/// </list>
/// </remarks>
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
    /// Logger for the MemoryCache operations.
    /// </summary>
    private readonly ILogger _logger;

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
    /// Initializes a new instance of the MemoryCache class.
    /// </summary>
    public MemoryCache() : this(NullLogger.Instance)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the MemoryCache class with a specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging cache operations.</param>
    public MemoryCache(ILogger logger)
    {
        _logger = logger;
        OnSet = delegate { };
        _logger.LogDebug("MemoryCache initialized");
    }

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
        _logger.LogDebug("Attempting to get value for key '{Key}' from memory cache", key);
        using (_lock.EnterScope())
        {
            bool found = _cache.TryGetValue(key, out value);
            if (found)
            {
                _logger.LogDebug("Successfully retrieved value for key '{Key}' from memory cache", key);
            }
            else
            {
                _logger.LogDebug("Key '{Key}' not found in memory cache", key);
            }
            return found;
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
        _logger.LogDebug("Setting value for key '{Key}' in memory cache", key);
        using (_lock.EnterScope())
        {
            _cache[key] = value;
            _logger.LogInformation("Successfully set value for key '{Key}' in memory cache", key);
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
        _logger.LogDebug("Removing value for key '{Key}' from memory cache", key);
        using (_lock.EnterScope())
        {
            bool removed = _cache.Remove(key);
            if (removed)
            {
                _logger.LogInformation("Successfully removed value for key '{Key}' from memory cache", key);
            }
            else
            {
                _logger.LogDebug("Key '{Key}' not found for removal in memory cache", key);
            }
            return removed;
        }
    }

    /// <summary>
    /// Clears all entries in the cache.
    /// This method is protected and is meant to be used internally by subclasses
    /// to remove all items from the underlying cache data structure.
    /// </summary>
    protected override void ClearInternal()
    {
        _logger.LogWarning("Clearing all entries from memory cache");
        using (_lock.EnterScope())
        {
            int count = _cache.Count;
            _cache.Clear();
            _logger.LogInformation("Successfully cleared {Count} entries from memory cache", count);
        }
    }

    /// <summary>
    /// Retrieves a collection of all keys currently stored in the cache.
    /// </summary>
    /// <returns>
    /// An enumerable containing all keys in the cache.
    /// </returns>
    public override IEnumerable<TKey> Keys()
    {
        _logger.LogDebug("Retrieving all keys from memory cache");
        using (_lock.EnterScope())
        {
            var keys = _cache.Keys.ToList();
            _logger.LogDebug("Retrieved {Count} keys from memory cache", keys.Count);
            return keys;
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
        _logger.LogDebug("Checking memory cache availability");
        TKey testKey = (TKey)(object)Guid.NewGuid().ToString(); // Works for string keys. For other types, provide a suitable unique key.
        TValue? testValue = default(TValue);
        try
        {
            using (_lock.EnterScope())
            {
                // Try to add and retrieve a dummy value
                _cache[testKey] = testValue;
                bool success = _cache.TryGetValue(testKey, out _);
                _cache.Remove(testKey);
                
                if (success)
                {
                    _logger.LogDebug("Memory cache is available");
                }
                else
                {
                    _logger.LogWarning("Memory cache availability check failed");
                }
                
                return success;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking memory cache availability");
            return false;
        }
    }
}
