using System.Text.Json;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// File-based cache implementation for generic values, supporting cache availability and key enumeration.
/// </summary>
/// <typeparam name="TValue">The type of values to be stored in the cache.</typeparam>
public class FileCache<TValue> : CacheBase<string, TValue>, ICacheAvailability, ICacheEnumerable<string>
{
    /// <summary>
    /// Represents the file system directory path where cached data is stored.
    /// </summary>
    /// <remarks>
    /// This directory path is used to save and retrieve cache entries from the file system.
    /// If the directory does not exist, it will be created upon initialization of the cache.
    /// </remarks>
    private readonly string _directoryPath;

    /// <summary>
    /// Synchronization object used to ensure thread safety when accessing or
    /// modifying the file-based cache. Locks the critical sections of code
    /// where file operations take place to avoid race conditions.
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// Represents a file-based caching system for storing and retrieving data using a file system,
    /// extending the functionality of the generic <see cref="CacheBase{TKey, TValue}"/> class.
    /// Provides additional capabilities such as enumeration over keys and availability checks.
    /// </summary>
    /// <typeparam name="TValue">The type of values to be stored in the cache.</typeparam>
    public FileCache(string directoryPath)
    {
        _directoryPath = directoryPath;
        if (!Directory.Exists(_directoryPath))
            Directory.CreateDirectory(_directoryPath);
    }

    /// <summary>
    /// Attempts to retrieve a value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The unique key identifying the cached value to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key,
    /// if the key was found in the cache; otherwise, the default value for the type of the value parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the specified key exists in the cache.
    /// Returns true if the key exists and the value is successfully retrieved; otherwise, false.
    /// </returns>
    protected override bool TryGetInternal(string key, out TValue value)
    {
        try
        {
            var filePath = GetFilePath(key);
            lock (_lock)
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    value = JsonSerializer.Deserialize<TValue>(json);
                    return true;
                }
            }
        }
        catch
        {
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Stores a key-value pair in the cache, handling the internal implementation logic for persisting data.
    /// </summary>
    /// <param name="key">The key associated with the value being stored in the cache.</param>
    /// <param name="value">The value to be stored in the cache.</param>
    protected override void SetInternal(string key, TValue value)
    {
        var filePath = GetFilePath(key);
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(value);
            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Removes the specified entry identified by the key from the cache storage.
    /// </summary>
    /// <param name="key">The key of the item to be removed from the cache.</param>
    /// <returns>
    /// A boolean value indicating whether the item was successfully removed.
    /// Returns true if the item existed and was removed; otherwise, false.
    /// </returns>
    protected override bool RemoveInternal(string key)
    {
        try
        {
            var filePath = GetFilePath(key);
            lock (_lock)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    /// <summary>
    /// Deletes all cached items by clearing the storage used for the cache.
    /// This implementation is specific to deleting all files from the directory
    /// associated with the file-based cache.
    /// </summary>
    /// <remarks>
    /// This method ensures thread safety by locking the operation during the
    /// file deletion process. Any exceptions encountered during the clearing
    /// operation are silently handled.
    /// </remarks>
    protected override void ClearInternal()
    {
        try
        {
            lock (_lock)
            {
                var files = Directory.GetFiles(_directoryPath);
                foreach (var file in files)
                    File.Delete(file);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Generates the full file path for a given cache key by combining it with the directory path and ensuring it is a safe filename.
    /// </summary>
    /// <param name="key">The key used to identify the cache entry.</param>
    /// <returns>The file path associated with the given key.</returns>
    private string GetFilePath(string key)
    {
        var safeFileName = Uri.EscapeDataString(key);
        return Path.Combine(_directoryPath, safeFileName + ".json");
    }

    /// <summary>
    /// Retrieves a collection of all keys currently stored in the cache.
    /// </summary>
    /// <returns>
    /// An enumerable collection of keys available in the cache.
    /// </returns>
    public IEnumerable<string> Keys()
    {
        lock (_lock)
        {
            return Directory.GetFiles(_directoryPath)
                .Select(Path.GetFileName)
                .Select(Uri.UnescapeDataString)
                .Select(name => name.Substring(0, name.Length - 5)) // remove .json
                .ToList();
        }
    }

    /// <summary>
    /// Determines whether the cache is currently available for operations.
    /// </summary>
    /// <returns>
    /// A boolean value indicating the availability of the cache.
    /// Returns <c>true</c> if the cache is available; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsAvailable()
    {
        try
        {
            // Check if the directory exists and is accessible
            if (!Directory.Exists(_directoryPath))
                return false;

            // Attempt to create a uniquely named temporary file to avoid deleting any existing file
            string testFileName = Guid.NewGuid().ToString("N") + ".tmp";
            string testFilePath = Path.Combine(_directoryPath, testFileName);

            // Write to the test file
            File.WriteAllText(testFilePath, "availability check");

            // Delete the test file
            File.Delete(testFilePath);

            return true;
        }
        catch
        {
            return false;
        }
    }
}