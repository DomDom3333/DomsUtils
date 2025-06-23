using DomsUtils.Services.Pipeline.BlockStorage;

namespace DomsUtils.Services.Pipeline.Plugins.Storage.Implementations;

/// <summary>
/// A storage implementation that combines multiple storage instances, trying each in sequence.
/// </summary>
/// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
/// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
public class LayeredBlockStorage<TKey, TValue> : IBlockStorage<TKey, TValue>
    where TKey : notnull
{
    private readonly List<IBlockStorage<TKey, TValue>> _storages;

    /// <summary>
    /// Creates a new layered storage with the specified storage instances.
    /// </summary>
    /// <param name="primaryStorage">The primary storage to try first.</param>
    /// <param name="fallbackStorage">The fallback storage to try if the primary fails.</param>
    public LayeredBlockStorage(IBlockStorage<TKey, TValue> primaryStorage, IBlockStorage<TKey, TValue> fallbackStorage)
    {
        _storages = new List<IBlockStorage<TKey, TValue>> { primaryStorage, fallbackStorage };
    }

    /// <summary>
    /// Creates a new layered storage with the specified storage instances.
    /// </summary>
    /// <param name="storages">The storage instances to use, in order of priority.</param>
    public LayeredBlockStorage(params IBlockStorage<TKey, TValue>[] storages)
    {
        if (storages == null || storages.Length == 0)
        {
            throw new ArgumentException("At least one storage must be provided", nameof(storages));
        }

        _storages = new List<IBlockStorage<TKey, TValue>>(storages);
    }

    /// <summary>
    /// Tries to get a value from any of the storage instances, starting with the highest priority.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The value if found, default otherwise.</param>
    /// <returns>True if the key exists in any storage, false otherwise.</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        foreach (var storage in _storages)
        {
            if (storage.TryGetValue(key, out value))
            {
                // Found in this storage, propagate to higher priority storages
                PropagateValueUp(key, value, storage);
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Sets a value in all storage instances.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store.</param>
    public void SetValue(TKey key, TValue value)
    {
        // Set in all storages
        foreach (var storage in _storages)
        {
            storage.SetValue(key, value);
        }
    }

    /// <summary>
    /// Removes a value from all storage instances.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <returns>True if the value was removed from any storage, false otherwise.</returns>
    public bool RemoveValue(TKey key)
    {
        bool removed = false;

        // Remove from all storages
        foreach (var storage in _storages)
        {
            removed |= storage.RemoveValue(key);
        }

        return removed;
    }

    /// <summary>
    /// Clears all storage instances.
    /// </summary>
    public void Clear()
    {
        // Clear all storages
        foreach (var storage in _storages)
        {
            storage.Clear();
        }
    }

    /// <summary>
    /// Propagates a value found in a lower priority storage to all higher priority storages.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <param name="value">The value to propagate.</param>
    /// <param name="sourceStorage">The storage where the value was found.</param>
    private void PropagateValueUp(TKey key, TValue value, IBlockStorage<TKey, TValue> sourceStorage)
    {
        int sourceIndex = _storages.IndexOf(sourceStorage);

        // Propagate to all higher priority storages
        for (int i = 0; i < sourceIndex; i++)
        {
            _storages[i].SetValue(key, value);
        }
    }
}
