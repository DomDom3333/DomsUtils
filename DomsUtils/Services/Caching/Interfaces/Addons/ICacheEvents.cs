namespace DomsUtils.Services.Caching.Interfaces.Addons;

/// <summary>
/// Defines an optional interface that provides event-driven notifications for Set operations in a cache.
/// This allows external subscribers, such as tiered caching mechanisms, to react to cache updates.
/// </summary>
/// <typeparam name="TKey">The type of the key used in the cache.</typeparam>
/// <typeparam name="TValue">The type of the value stored in the cache.</typeparam>
public interface ICacheEvents<out TKey, out TValue>
{
    /// <summary>
    /// Event triggered whenever a value is set in the cache.
    /// </summary>
    event Action<TKey, TValue> OnSet;
}