namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// A storage implementation that automatically expires entries after a specified time.
/// </summary>
/// <typeparam name="TKey">The type of keys used for storage access.</typeparam>
/// <typeparam name="TValue">The type of values stored in the storage.</typeparam>
public class ExpiringBlockStorage<TKey, TValue> : IBlockStorage<TKey, TValue>, IAsyncDisposable
    where TKey : notnull
{
    private readonly IBlockStorage<TKey, TValue> _baseStorage;
    private readonly Dictionary<TKey, DateTime> _expirationTimes;
    private readonly TimeSpan _expiration;
    private readonly Timer _cleanupTimer;
    private readonly object _lock = new object();

    /// <summary>
    /// Creates a new expiring storage with the specified expiration time.
    /// </summary>
    /// <param name="baseStorage">The underlying storage to use.</param>
    /// <param name="expiration">The time after which entries should expire.</param>
    public ExpiringBlockStorage(IBlockStorage<TKey, TValue> baseStorage, TimeSpan expiration)
    {
        _baseStorage = baseStorage ?? throw new ArgumentNullException(nameof(baseStorage));
        _expiration = expiration;
        _expirationTimes = new Dictionary<TKey, DateTime>();

        // Set up a timer to periodically clean up expired entries
        _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Tries to get a value if it hasn't expired.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The value if found and not expired, default otherwise.</param>
    /// <returns>True if the key exists and hasn't expired, false otherwise.</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            // Check if the key has expired
            if (_expirationTimes.TryGetValue(key, out var expirationTime) && 
                DateTime.UtcNow > expirationTime)
            {
                // Remove expired entry
                _expirationTimes.Remove(key);
                _baseStorage.RemoveValue(key);
                value = default;
                return false;
            }

            // Get the value from the base storage
            return _baseStorage.TryGetValue(key, out value);
        }
    }

    /// <summary>
    /// Sets a value with an expiration time.
    /// </summary>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store.</param>
    public void SetValue(TKey key, TValue value)
    {
        lock (_lock)
        {
            // Set the value in the base storage
            _baseStorage.SetValue(key, value);

            // Set or update the expiration time
            _expirationTimes[key] = DateTime.UtcNow.Add(_expiration);
        }
    }

    /// <summary>
    /// Removes a value and its expiration time.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <returns>True if the value was removed, false otherwise.</returns>
    public bool RemoveValue(TKey key)
    {
        lock (_lock)
        {
            // Remove the expiration time
            _expirationTimes.Remove(key);

            // Remove the value from the base storage
            return _baseStorage.RemoveValue(key);
        }
    }

    /// <summary>
    /// Clears all values and expiration times.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            // Clear all expiration times
            _expirationTimes.Clear();

            // Clear the base storage
            _baseStorage.Clear();
        }
    }

    /// <summary>
    /// Cleans up entries that have expired.
    /// </summary>
    private void CleanupExpiredEntries()
    {
        List<TKey> expiredKeys = new List<TKey>();
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            // Find all expired keys
            foreach (var entry in _expirationTimes)
            {
                if (now > entry.Value)
                {
                    expiredKeys.Add(entry.Key);
                }
            }

            // Remove expired entries
            foreach (var key in expiredKeys)
            {
                _expirationTimes.Remove(key);
                _baseStorage.RemoveValue(key);
            }
        }
    }

    /// <summary>
    /// Disposes the cleanup timer.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _cleanupTimer.DisposeAsync();

        if (_baseStorage is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_baseStorage is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
