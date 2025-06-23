using System.Collections.Concurrent;
using DomsUtils.Services.Pipeline.BlockStorage;

namespace DomsUtils.Services.Pipeline.Plugins.Storage.Implementations;

/// <summary>
/// Default in-memory implementation of <see cref="IBlockStorage{TKey,TValue}"/> using a thread-safe dictionary.
/// </summary>
/// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
/// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
public class InMemoryBlockStorage<TKey, TValue> : IBlockStorage<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _storage = new();

    /// <summary>
    /// Provides an in-memory implementation of the <see cref="IBlockStorage{TKey, TValue}"/> interface.
    /// This storage solution stores key-value pairs entirely in system memory, offering
    /// fast read and write operations. It is ideal for scenarios where persistence is
    /// not required, and data needs to be quickly accessible during the lifetime of
    /// an application.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for storage. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the value to be stored.</typeparam>
    public InMemoryBlockStorage(){}

    /// <summary>
    /// Provides an in-memory implementation of the <see cref="IBlockStorage{TKey, TValue}"/> interface,
    /// utilizing a thread-safe dictionary to store key-value pairs.
    /// This implementation offers high-performance operations for scenarios where
    /// data persistence is not required and memory storage is sufficient for application needs.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys used for accessing stored values. Keys must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values to be stored and managed.</typeparam>
    public InMemoryBlockStorage(int initialCapacity)
    {
        _storage = new ConcurrentDictionary<TKey, TValue>(Environment.ProcessorCount, initialCapacity);
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue? value)
    {
        return _storage.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public void SetValue(TKey key, TValue value)
    {
        _storage[key] = value;
    }

    /// <inheritdoc />
    public bool RemoveValue(TKey key)
    {
        return _storage.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _storage.Clear();
    }
}
