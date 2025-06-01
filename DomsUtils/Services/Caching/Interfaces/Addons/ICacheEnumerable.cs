namespace DomsUtils.Services.Caching.Interfaces.Addons;

/// <summary>
/// Interface defining the ability to enumerate stored keys in a cache.
/// Typically used for operations where iteration over all cache keys is required,
/// such as migrations, analytics, or maintenance tasks.
/// </summary>
public interface ICacheEnumerable<out TKey>
{
    /// <summary>
    /// Retrieves a collection of all keys currently stored in the cache.
    /// </summary>
    /// <returns>
    /// An enumerable collection of keys available in the cache.
    /// </returns>
    IEnumerable<TKey> Keys();
}