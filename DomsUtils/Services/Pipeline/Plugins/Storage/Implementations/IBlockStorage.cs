namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// Provides shared storage that can be accessed by blocks in a pipeline.
/// This allows blocks to persist and retrieve data across multiple processing steps.
/// </summary>
/// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
/// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
public interface IBlockStorage<TKey, TValue>
{
    /// <summary>
    /// Tries to get a value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The value if found, default otherwise.</param>
    /// <returns>True if the key exists in storage, false otherwise.</returns>
    bool TryGetValue(TKey key, out TValue? value);

    /// <summary>
    /// Sets a value with the specified key in the storage.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store.</param>
    void SetValue(TKey key, TValue value);

    /// <summary>
    /// Removes a value with the specified key from the storage.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <returns>True if the value was removed, false if the key was not found.</returns>
    bool RemoveValue(TKey key);

    /// <summary>
    /// Clears all values from the storage.
    /// </summary>
    void Clear();
}
