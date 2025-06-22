namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// Extension methods for working with block storage in a pipeline.
/// </summary>
public static class BlockStorageExtensions
{
    /// <summary>
    /// Adds named storage capability to a pipeline, allowing blocks to share data.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="pipeline">The pipeline to enhance with storage.</param>
    /// <param name="storageName">The name to identify this storage instance.</param>
    /// <param name="storage">The storage implementation to use. If null, creates a new in-memory storage.</param>
    /// <returns>The same pipeline instance for method chaining.</returns>
    public static ChannelPipeline<T> WithStorage<T, TKey, TValue>(
        this ChannelPipeline<T> pipeline,
        string storageName,
        IBlockStorage<TKey, TValue>? storage = null) where TKey : notnull
    {
        var key = new StorageKey<TKey, TValue>(storageName);
        var actualStorage = storage ?? new InMemoryBlockStorage<TKey, TValue>();
        PipelineStorageRegistry.RegisterStorage(pipeline, key, actualStorage);

        // Register storage resource to handle cleanup when pipeline is disposed
        PipelineResourceRegistry.RegisterResource(pipeline, new StorageResource());

        return pipeline;
    }

    /// <summary>
    /// Adds default storage capability to a pipeline, allowing blocks to share data.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="pipeline">The pipeline to enhance with storage.</param>
    /// <param name="storage">The storage implementation to use. If null, creates a new in-memory storage.</param>
    /// <returns>The same pipeline instance for method chaining.</returns>
    public static ChannelPipeline<T> WithStorage<T, TKey, TValue>(
        this ChannelPipeline<T> pipeline,
        IBlockStorage<TKey, TValue>? storage = null) where TKey : notnull
    {
        var actualStorage = storage ?? new InMemoryBlockStorage<TKey, TValue>();
        PipelineStorageRegistry.RegisterStorage(pipeline, actualStorage);

        // Register storage resource to handle cleanup when pipeline is disposed
        PipelineResourceRegistry.RegisterResource(pipeline, new StorageResource());

        return pipeline;
    }

    /// <summary>
    /// Retrieves a named storage instance associated with a pipeline.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="pipeline">The pipeline to get storage from.</param>
    /// <param name="storageName">The name of the storage to retrieve.</param>
    /// <returns>The storage instance or null if none is registered with the given name.</returns>
    public static IBlockStorage<TKey, TValue>? GetStorage<TKey, TValue>(
        this object pipeline,
        string storageName) where TKey : notnull
    {
        var key = new StorageKey<TKey, TValue>(storageName);
        return PipelineStorageRegistry.GetStorage(pipeline, key);
    }

    /// <summary>
    /// Retrieves the default storage associated with a pipeline.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
    /// <param name="pipeline">The pipeline to get storage from.</param>
    /// <returns>The storage instance or null if none is registered.</returns>
    public static IBlockStorage<TKey, TValue>? GetStorage<TKey, TValue>(
        this object pipeline) where TKey : notnull
    {
        return PipelineStorageRegistry.GetStorage<TKey, TValue>(pipeline);
    }
}
