using System.Collections.Concurrent;

namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// Registry that manages storage instances associated with pipelines.
/// </summary>
/// <remarks>
/// This registry allows blocks to access shared storage across a pipeline.
/// Storage is associated with pipeline instances and automatically cleaned up
/// when pipelines are disposed.
/// </remarks>
internal static class PipelineStorageRegistry
{
    private static readonly ConcurrentDictionary<object, ConcurrentDictionary<string, object>> _storageMap = new();

    /// <summary>
    /// Registers a storage instance with a pipeline using a specific key.
    /// </summary>
    /// <param name="pipeline">The pipeline to associate storage with.</param>
    /// <param name="key">The key to identify this storage instance.</param>
    /// <param name="storage">The storage instance to register.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    internal static bool RegisterStorage<TKey, TValue>(object pipeline, StorageKey<TKey, TValue> key, IBlockStorage<TKey, TValue> storage)
    {
        var pipelineStorages = _storageMap.GetOrAdd(pipeline, _ => new ConcurrentDictionary<string, object>());
        return pipelineStorages.TryAdd(key.ToString(), storage);
    }

    /// <summary>
    /// Registers a storage instance with a pipeline using a default key based on the storage types.
    /// </summary>
    /// <param name="pipeline">The pipeline to associate storage with.</param>
    /// <param name="storage">The storage instance to register.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    internal static bool RegisterStorage<TKey, TValue>(object pipeline, IBlockStorage<TKey, TValue> storage)
    {
        var defaultKey = new StorageKey<TKey, TValue>("default");
        return RegisterStorage(pipeline, defaultKey, storage);
    }

    /// <summary>
    /// Retrieves a storage instance associated with a pipeline using the specified key.
    /// </summary>
    /// <param name="pipeline">The pipeline to get storage from.</param>
    /// <param name="key">The key identifying the storage instance.</param>
    /// <returns>The associated storage instance or null if none exists.</returns>
    internal static IBlockStorage<TKey, TValue>? GetStorage<TKey, TValue>(object pipeline, StorageKey<TKey, TValue> key)
    {
        if (_storageMap.TryGetValue(pipeline, out var pipelineStorages) && 
            pipelineStorages.TryGetValue(key.ToString(), out var storage) &&
            storage is IBlockStorage<TKey, TValue> typedStorage)
        {
            return typedStorage;
        }

        return null;
    }

    /// <summary>
    /// Retrieves a storage instance associated with a pipeline using the default key.
    /// </summary>
    /// <typeparam name="TKey">The key type of the storage.</typeparam>
    /// <typeparam name="TValue">The value type of the storage.</typeparam>
    /// <param name="pipeline">The pipeline to get storage from.</param>
    /// <returns>The associated storage instance or null if none exists.</returns>
    internal static IBlockStorage<TKey, TValue>? GetStorage<TKey, TValue>(object pipeline)
    {
        var defaultKey = new StorageKey<TKey, TValue>("default");
        return GetStorage(pipeline, defaultKey);
    }

    /// <summary>
    /// Removes all storage instances associated with a pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to clean up storage for.</param>
    internal static void CleanupPipeline(object pipeline)
    {
        _storageMap.TryRemove(pipeline, out _);
    }
}
