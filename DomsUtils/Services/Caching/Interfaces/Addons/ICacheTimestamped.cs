using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Interfaces.Addons;

/// <summary>
/// Represents a timestamped cache interface that extends the base cache functionality
/// to provide support for retaining the associated timestamp when storing or retrieving key-value pairs.
/// </summary>
/// <typeparam name="TKey">The type of keys stored in the cache.</typeparam>
/// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
public interface ICacheTimestamped<TKey, TValue> : ICache<TKey, TValue>
{
    /// <summary>
    /// Attempts to retrieve the value and its associated timestamp from the cache for the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">The value associated with the specified key, if found. This parameter is passed uninitialized.</param>
    /// <param name="timestamp">The timestamp associated with the value, if found. This parameter is passed uninitialized.</param>
    /// <returns>
    /// true if the key exists and both the value and timestamp were successfully retrieved; otherwise, false.
    /// </returns>
    bool TryGetWithTimestamp(TKey key, out TValue value, out DateTimeOffset timestamp);

    /// <summary>
    /// Sets the specified key and value in the cache along with a timestamp.
    /// </summary>
    /// <param name="key">The key to associate with the value in the cache.</param>
    /// <param name="value">The value to be stored in the cache associated with the specified key.</param>
    /// <param name="timestamp">The timestamp representing the time the value was added to the cache.</param>
    void SetWithTimestamp(TKey key, TValue value, DateTimeOffset timestamp);
}