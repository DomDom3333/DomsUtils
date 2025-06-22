namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// Extension methods for working with storage in a more generic way, without direct pipeline dependencies.
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    /// Creates a new in-memory storage instance.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <returns>A new in-memory storage instance.</returns>
    public static IBlockStorage<TKey, TValue> CreateInMemoryStorage<TKey, TValue>() where TKey : notnull
    {
        return new InMemoryBlockStorage<TKey, TValue>();
    }

    /// <summary>
    /// Creates a storage key to identify a specific storage instance.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="name">The name to identify this storage instance.</param>
    /// <returns>A storage key that can be used to retrieve the storage instance.</returns>
    public static StorageKey<TKey, TValue> CreateKey<TKey, TValue>(string name) where TKey : notnull
    {
        return new StorageKey<TKey, TValue>(name);
    }

    /// <summary>
    /// Creates a storage plugin that can be attached to a pipeline using a specific name.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="storage">The storage implementation to use. If null, creates a new in-memory storage.</param>
    /// <param name="name">The name to identify this storage instance.</param>
    /// <returns>A storage plugin that can be attached to a pipeline.</returns>
    public static StoragePlugin<T, TKey, TValue> AsPlugin<T, TKey, TValue>(
        this IBlockStorage<TKey, TValue>? storage,
        string name) where TKey : notnull
    {
        return new StoragePlugin<T, TKey, TValue>(name, storage);
    }

    /// <summary>
    /// Creates a storage plugin that can be attached to a pipeline using the default name.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="storage">The storage implementation to use. If null, creates a new in-memory storage.</param>
    /// <returns>A storage plugin that can be attached to a pipeline.</returns>
    public static StoragePlugin<T, TKey, TValue> AsPlugin<T, TKey, TValue>(
        this IBlockStorage<TKey, TValue>? storage) where TKey : notnull
    {
        return new StoragePlugin<T, TKey, TValue>(storage);
    }
}
