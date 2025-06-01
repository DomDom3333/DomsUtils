namespace DomsUtils.Services.Caching.Interfaces.Bases;

/// <summary>
/// Represents a generic cache interface with basic operations for managing key-value pairs.
/// </summary>
public interface ICache<in TKey, TValue>
{
    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">The value associated with the specified key, if found. This parameter is passed uninitialized.</param>
    /// <returns>
    /// true if the key exists and the value was successfully retrieved; otherwise, false.
    /// </returns>
    bool TryGet(TKey key, out TValue value);

    /// <summary>
    /// Sets the specified key and value in the cache.
    /// </summary>
    /// <param name="key">The key to associate with the value in the cache.</param>
    /// <param name="value">The value to be stored in the cache associated with the specified key.</param>
    void Set(TKey key, TValue value);

    /// <summary>
    /// Removes the item associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the item to remove from the cache.</param>
    /// <returns>A boolean value indicating whether the item was successfully removed.</returns>
    bool Remove(TKey key);

    /// <summary>
    /// Clears all items from the cache.
    /// This operation removes all key-value pairs stored in the cache.
    /// </summary>
    void Clear();
}