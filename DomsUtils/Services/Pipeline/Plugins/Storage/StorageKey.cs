namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// Provides a way to identify and retrieve storage instances by name rather than type.
/// </summary>
/// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
/// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
public class StorageKey<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Gets the name of this storage instance.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a new storage key with the specified name.
    /// </summary>
    /// <param name="name">The name to identify this storage instance.</param>
    public StorageKey(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Gets a string representation of this storage key.
    /// </summary>
    /// <returns>A string representation of this storage key.</returns>
    public override string ToString() => $"Storage<{typeof(TKey).Name},{typeof(TValue).Name}>:{Name}";
}
