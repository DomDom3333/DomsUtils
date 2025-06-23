using DomsUtils.Services.Pipeline.Plugins.Storage.Implementations;

namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// Factory class for creating different types of storage instances with a fluent API.
/// </summary>
public static class StorageFactory
{
    /// <summary>
    /// Creates a new in-memory storage instance.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <returns>A new in-memory storage instance.</returns>
    public static IBlockStorage<TKey, TValue> InMemory<TKey, TValue>() where TKey : notnull
    {
        return new InMemoryBlockStorage<TKey, TValue>();
    }

    /// <summary>
    /// Creates a new in-memory storage instance with the specified initial capacity.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="initialCapacity">The initial capacity of the storage dictionary.</param>
    /// <returns>A new in-memory storage instance with the specified capacity.</returns>
    public static IBlockStorage<TKey, TValue> InMemory<TKey, TValue>(int initialCapacity) where TKey : notnull
    {
        return new InMemoryBlockStorage<TKey, TValue>(initialCapacity);
    }

    /// <summary>
    /// Creates a storage that combines multiple storage instances, trying each in sequence.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="primaryStorage">The primary storage to try first.</param>
    /// <param name="fallbackStorage">The fallback storage to try if the primary fails.</param>
    /// <returns>A storage that combines multiple storage instances.</returns>
    public static IBlockStorage<TKey, TValue> Layered<TKey, TValue>(
        IBlockStorage<TKey, TValue> primaryStorage,
        IBlockStorage<TKey, TValue> fallbackStorage) where TKey : notnull
    {
        return new LayeredBlockStorage<TKey, TValue>(primaryStorage, fallbackStorage);
    }

    /// <summary>
    /// Creates a storage that automatically expires entries after a specified time.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="baseStorage">The underlying storage to use.</param>
    /// <param name="expiration">The time after which entries should expire.</param>
    /// <returns>A storage with automatic expiration of entries.</returns>
    public static IBlockStorage<TKey, TValue> WithExpiration<TKey, TValue>(
        IBlockStorage<TKey, TValue>? baseStorage,
        TimeSpan expiration) where TKey : notnull
    {
        return new ExpiringBlockStorage<TKey, TValue>(baseStorage ?? InMemory<TKey, TValue>(), expiration);
    }
}
