using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// Abstract base class for implementing caching mechanisms. Provides the core methods for
/// accessing, modifying, and clearing cached data through abstract internal operations.
/// Derived classes are responsible for implementing specific caching behavior.
/// </summary>
/// <typeparam name="TKey">The type of the key for identifying cached entries.</typeparam>
/// <typeparam name="TValue">The type of the value to be stored in the cache.</typeparam>
public abstract class CacheBase<TKey, TValue> : ICache<TKey, TValue>, ICacheAvailability
{
    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">The value associated with the specified key, if found. This parameter is passed uninitialized.</param>
    /// <returns>
    /// true if the key exists and the value was successfully retrieved; otherwise, false.
    /// </returns>
    public virtual bool TryGet(TKey key, out TValue value) => TryGetInternal(key, out value);

    /// <summary>
    /// Sets the specified key and value in the cache. This method may trigger implementation-specific behaviors such as events or other operations.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store in the cache associated with the key.</param>
    public virtual void Set(TKey key, TValue value) => SetInternal(key, value);

    /// <summary>
    /// Removes the item associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <returns>A boolean value indicating whether the removal was successful.</returns>
    public virtual bool Remove(TKey key) => RemoveInternal(key);

    /// <summary>
    /// Clears all items from the cache. Removes all stored key-value pairs.
    /// </summary>
    public virtual void Clear() => ClearInternal();

    /// <summary>
    /// Attempts to retrieve a value associated with the specified key from the cache.
    /// This method is intended to be implemented by derived classes and provides specific storage logic.
    /// </summary>
    /// <param name="key">The key associated with the value to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, contains the default value for the type of the value parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// True if the key exists in the cache and the value is successfully retrieved; otherwise, false.
    /// </returns>
    protected abstract bool TryGetInternal(TKey key, out TValue value);

    /// <summary>
    /// Abstract method to set a value in the cache for a specified key. This method must be implemented
    /// by derived classes to define specific storage behavior.
    /// </summary>
    /// <param name="key">The key identifying the cache entry to set.</param>
    /// <param name="value">The value to store in the cache associated with the specified key.</param>
    protected abstract void SetInternal(TKey key, TValue value);

    /// <summary>
    /// Removes the cache entry associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the cache entry to remove.</param>
    /// <returns>
    /// true if the key was successfully removed from the cache; otherwise, false.
    /// </returns>
    protected abstract bool RemoveInternal(TKey key);

    /// <summary>
    /// Implements the logic for clearing all entries in the cache.
    /// This method is used internally by the base class and derived classes to define
    /// their specific behavior for removing all stored data from the underlying storage mechanism.
    /// </summary>
    /// <remarks>
    /// Derived classes must provide specific implementation details for clearing the cache,
    /// depending on the underlying storage such as in-memory, file-based, or external services.
    /// </remarks>
    protected abstract void ClearInternal();

    /// <summary>
    /// Determines whether the cache is currently available for use.
    /// </summary>
    /// <returns>
    /// true if the cache is available and operational; otherwise, false.
    /// </returns>
    public abstract bool IsAvailable();
}