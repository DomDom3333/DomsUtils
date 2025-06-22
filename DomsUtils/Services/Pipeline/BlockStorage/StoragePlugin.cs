using DomsUtils.Services.Pipeline.PipelinePlugin;

namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// A plugin that provides storage functionality to a pipeline.
/// </summary>
/// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
/// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
/// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
public class StoragePlugin<T, TKey, TValue> : IPipelinePlugin<T> where TKey : notnull
{
    private readonly IBlockStorage<TKey, TValue> _storage;
    private readonly StorageKey<TKey, TValue> _key;

    /// <summary>
    /// Gets the underlying storage instance.
    /// </summary>
    public IBlockStorage<TKey, TValue> Storage => _storage;

    /// <summary>
    /// Gets the storage key used to identify this storage instance.
    /// </summary>
    public StorageKey<TKey, TValue> Key => _key;

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name => _key.ToString();

    /// <summary>
    /// Creates a new storage plugin with the specified storage implementation and name.
    /// </summary>
    /// <param name="storageName">The name to identify this storage instance.</param>
    /// <param name="storage">The storage implementation to use. If null, creates a new in-memory storage.</param>
    public StoragePlugin(string storageName, IBlockStorage<TKey, TValue>? storage = null)
    {
        _key = new StorageKey<TKey, TValue>(storageName);
        _storage = storage ?? new InMemoryBlockStorage<TKey, TValue>();
    }

    /// <summary>
    /// Creates a new storage plugin with the specified storage implementation and a default name.
    /// </summary>
    /// <param name="storage">The storage implementation to use. If null, creates a new in-memory storage.</param>
    public StoragePlugin(IBlockStorage<TKey, TValue>? storage = null)
        : this("default", storage)
    {
    }

    /// <summary>
    /// Called when the plugin is attached to a pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to which the plugin is being attached.</param>
    public void OnAttach(ChannelPipeline<T> pipeline)
    {
        // Register the storage with the pipeline using the key
        PipelineStorageRegistry.RegisterStorage(pipeline, _key, _storage);
    }

    /// <summary>
    /// Called when the pipeline is being disposed.
    /// </summary>
    /// <param name="pipeline">The pipeline that is being disposed.</param>
    public void OnDispose(ChannelPipeline<T> pipeline)
    {
        // If the storage is disposable, dispose it
        if (_storage is IAsyncDisposable asyncDisposable)
        {
            // Fire and forget disposal
            _ = asyncDisposable.DisposeAsync();
        }
        else if (_storage is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
