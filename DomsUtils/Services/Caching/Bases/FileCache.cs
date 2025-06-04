using System.Text.Json;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// Represents a file-based caching system for storing and retrieving objects of a specified type,
/// using generic keys of any type.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used to identify cache entries. Must be non-null.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of values to be stored in the cache.
/// </typeparam>
/// <remarks>
/// This class extends <see cref="CacheBase{TKey, TValue}"/> and implements <see cref="ICacheAvailability"/>
/// and <see cref="ICacheEnumerable{TKey}"/> to provide functionality for checking cache availability
/// and enumerating stored keys.
/// </remarks>
public class FileCache<TKey, TValue> : ICache<TKey, TValue>, ICacheAvailability, ICacheEnumerable<TKey>
    where TKey : notnull
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
    private readonly Lock _lock = new Lock();
    
    /// <summary>
    /// Dictionary that maps keys to their corresponding filenames.
    /// This allows for arbitrary key types to be used with the file cache system.
    /// </summary>
    private readonly Dictionary<TKey, string> _keyToFileMap = new Dictionary<TKey, string>();
    
    /// <summary>
    /// Path to the metadata file that stores the key-to-file mapping
    /// </summary>
    private readonly string _metadataFilePath;
    
    /// <summary>
    /// Logger for the FileCache operations
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Represents a serializable key-filename mapping entry
    /// </summary>
    private sealed record KeyMappingEntry(string SerializedKey, string Filename, string KeyTypeName);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCache{TKey,TValue}"/> class
    /// with the specified directory path for file storage.
    /// </summary>
    /// <param name="directoryPath">The directory path where cache files will be stored.</param>
    /// <param name="logger">Optional logger for logging cache operations.</param>
    public FileCache(string directoryPath, ILogger? logger = null)
    {
        _directoryPath = directoryPath;
        _logger = logger ?? NullLogger.Instance;
        
        _logger.LogInformation("Initializing FileCache with directory '{DirectoryPath}'", directoryPath);
        
        if (!Directory.Exists(_directoryPath))
        {
            _logger.LogDebug("Creating directory '{DirectoryPath}' for FileCache", _directoryPath);
            Directory.CreateDirectory(_directoryPath);
        }
            
        _metadataFilePath = Path.Combine(_directoryPath, "_keymapping.json");
        
        // Load existing key-to-file mapping if available
        LoadKeyMappings();
    }
    
    /// <summary>
    /// Loads the key-to-file mapping from the metadata file if it exists.
    /// </summary>
    private void LoadKeyMappings()
    {
        using (_lock.EnterScope())
        {
            if (!File.Exists(_metadataFilePath))
            {
                _logger.LogDebug("Key mapping file not found at '{MetadataFilePath}'", _metadataFilePath);
                return;
            }

            try
            {
                _logger.LogDebug("Loading key mappings from '{MetadataFilePath}'", _metadataFilePath);
                List<KeyMappingEntry>? mappings = LoadMappingsFromFile();
                if (mappings != null)
                {
                    _keyToFileMap.Clear();
                    ProcessMappings(mappings);
                    _logger.LogInformation("Successfully loaded {Count} key mappings", _keyToFileMap.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading key mappings from '{MetadataFilePath}'", _metadataFilePath);
                // If there's an error loading the mappings, start with an empty map
                _keyToFileMap.Clear();
            }
        }
    }

    /// <summary>
    /// Loads and deserializes mappings from the metadata file.
    /// </summary>
    /// <returns>The deserialized mappings, or null if deserialization fails.</returns>
    private List<KeyMappingEntry>? LoadMappingsFromFile()
    {
        string json = File.ReadAllText(_metadataFilePath);
        return JsonSerializer.Deserialize<List<KeyMappingEntry>>(json);
    }

    /// <summary>
    /// Processes a collection of mapping entries and adds valid ones to the key-to-file map.
    /// </summary>
    /// <param name="mappings">The mappings to process.</param>
    private void ProcessMappings(List<KeyMappingEntry> mappings)
    {
        foreach (KeyMappingEntry mapping in mappings)
        {
            if (IsValidMapping(mapping))
            {
                TKey? key = TryDeserializeKey(mapping.SerializedKey);
                if (key is not null)
                {
                    _keyToFileMap[key] = mapping.Filename;
                    _logger.LogDebug("Added mapping for file '{Filename}'", mapping.Filename);
                }
            }
            else
            {
                _logger.LogWarning("Invalid mapping for file '{Filename}' with key type '{KeyTypeName}'", 
                    mapping.Filename, mapping.KeyTypeName);
            }
        }
    }

    /// <summary>
    /// Validates whether a mapping entry is valid (file exists and key type matches).
    /// </summary>
    /// <param name="mapping">The mapping to validate.</param>
    /// <returns>True if the mapping is valid, false otherwise.</returns>
    private bool IsValidMapping(KeyMappingEntry mapping)
    {
        string filePath = Path.Combine(_directoryPath, mapping.Filename);
        return File.Exists(filePath) && mapping.KeyTypeName == typeof(TKey).FullName;
    }

    /// <summary>
    /// Attempts to deserialize a key from its JSON representation.
    /// </summary>
    /// <param name="serializedKey">The serialized key.</param>
    /// <returns>The deserialized key, or null if deserialization fails.</returns>
    private static TKey? TryDeserializeKey(string serializedKey)
    {
        try
        {
            TKey? key = JsonSerializer.Deserialize<TKey>(serializedKey);
            return !Equals(key, default!) ? key : default;
        }
        catch
        {
            return default;
        }
    }
    
    /// <summary>
    /// Saves the current key-to-file mapping to the metadata file.
    /// </summary>
    private void SaveKeyMappings()
    {
        using (_lock.EnterScope())
        {
            try
            {
                _logger.LogDebug("Saving key mappings to '{MetadataFilePath}'", _metadataFilePath);
                // Create a serializable version of the mapping
                List<KeyMappingEntry> serializableMap = new List<KeyMappingEntry>();
                
                foreach (KeyValuePair<TKey, string> kvp in _keyToFileMap)
                {
                    try
                    {
                        // Serialize the key as JSON to preserve its structure
                        string serializedKey = JsonSerializer.Serialize(kvp.Key);
                        string keyTypeName = typeof(TKey).FullName ?? throw new InvalidOperationException("Key type name cannot be null");
                        
                        serializableMap.Add(new KeyMappingEntry(serializedKey, kvp.Value, keyTypeName));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to serialize key for filename '{Filename}'", kvp.Value);
                        // Skip keys that can't be serialized
                    }
                }
                
                string json = JsonSerializer.Serialize(serializableMap);
                File.WriteAllText(_metadataFilePath, json);
                _logger.LogInformation("Successfully saved {Count} key mappings", serializableMap.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving key mappings to '{MetadataFilePath}'", _metadataFilePath);
            }
        }
    }
    
    /// <summary>
    /// Gets or creates a unique filename for the given key.
    /// </summary>
    /// <param name="key">The key to get a filename for.</param>
    /// <returns>The filename associated with the key.</returns>
    private string GetOrCreateFilename(TKey key)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        
        using (_lock.EnterScope())
        {
            // If we already have a mapping for this key, return it
            if (_keyToFileMap.TryGetValue(key, out string? existingFilename))
            {
                _logger.LogDebug("Using existing filename '{Filename}' for key", existingFilename);
                return existingFilename;
            }
            
            // Otherwise, create a new unique filename
            string filename = Guid.NewGuid().ToString("N") + ".json";
            _keyToFileMap[key] = filename;
            _logger.LogDebug("Created new filename '{Filename}' for key", filename);
            
            // Save the updated mapping
            SaveKeyMappings();
            
            return filename;
        }
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
    protected bool TryGetInternal(TKey key, out TValue? value)
    {
        value = default!;

        try
        {
            _logger.LogDebug("Attempting to get value for key from file cache");
            using (_lock.EnterScope())
            {
                // Check if we have a filename for this key
                if (!_keyToFileMap.TryGetValue(key, out string? filename))
                {
                    _logger.LogDebug("No filename found for key in file cache");
                    return false;
                }
                
                string filePath = Path.Combine(_directoryPath, filename);
                _logger.LogDebug("Looking for cached file at '{FilePath}'", filePath);
                
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    value = JsonSerializer.Deserialize<TValue>(json);
                    _logger.LogDebug("Successfully retrieved value from file '{FilePath}'", filePath);
                    return true;
                }
                else
                {
                    // File was deleted outside of our control
                    _logger.LogWarning("Cache file '{FilePath}' was not found", filePath);
                    _keyToFileMap.Remove(key);
                    SaveKeyMappings();
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key from file cache");
            return false;
        }
    }

    /// <summary>
    /// Stores a key-value pair in the cache, handling the internal implementation logic for persisting data.
    /// </summary>
    /// <param name="key">The key associated with the value being stored in the cache.</param>
    /// <param name="value">The value to be stored in the cache.</param>
    protected void SetInternal(TKey key, TValue value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        
        using (_lock.EnterScope())
        {
            try
            {
                _logger.LogDebug("Setting value for key in file cache");
                
                // Get or create a filename for this key
                string filename = GetOrCreateFilename(key);
                string filePath = Path.Combine(_directoryPath, filename);
                
                // Serialize and save the value
                string json = JsonSerializer.Serialize(value);
                File.WriteAllText(filePath, json);
                
                _logger.LogInformation("Successfully saved value to file '{FilePath}'", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value for key in file cache");
            }
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
    protected bool RemoveInternal(TKey key)
    {
        try
        {
            _logger.LogDebug("Attempting to remove value for key from file cache");
            
            using (_lock.EnterScope())
            {
                // Check if we have a filename for this key
                if (!_keyToFileMap.TryGetValue(key, out string? filename))
                {
                    _logger.LogDebug("No filename found for key to remove");
                    return false;
                }
                
                string filePath = Path.Combine(_directoryPath, filename);
                _logger.LogDebug("Removing cached file at '{FilePath}'", filePath);
                
                // Remove the file if it exists
                bool fileRemoved = false;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    fileRemoved = true;
                    _logger.LogInformation("Successfully deleted file '{FilePath}'", filePath);
                }
                else
                {
                    _logger.LogWarning("File '{FilePath}' not found for deletion", filePath);
                }
                
                // Remove from our mapping
                _keyToFileMap.Remove(key);
                SaveKeyMappings();
                
                return fileRemoved;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value for key from file cache");
            return false;
        }
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
    protected void ClearInternal()
    {
        try
        {
            _logger.LogWarning("Clearing all files from cache directory '{DirectoryPath}'", _directoryPath);
            
            using (_lock.EnterScope())
            {
                // Delete all cache files except the metadata file
                IEnumerable<string> files = Directory.GetFiles(_directoryPath)
                    .Where(f => Path.GetFileName(f) != Path.GetFileName(_metadataFilePath));
                
                int count = 0;
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        count++;
                        _logger.LogDebug("Deleted cache file '{FilePath}'", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete cache file '{FilePath}'", file);
                    }
                }
                
                // Clear the key mapping
                _keyToFileMap.Clear();
                SaveKeyMappings();
                
                _logger.LogInformation("Successfully cleared {Count} files from cache directory", count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing files from cache directory '{DirectoryPath}'", _directoryPath);
        }
    }

    /// <summary>
    /// Retrieves a collection of all keys currently stored in the cache.
    /// </summary>
    /// <returns>
    /// An enumerable collection of keys available in the cache.
    /// </returns>
    public IEnumerable<TKey> Keys()
    {
        _logger.LogDebug("Retrieving all keys from file cache");
        
        using (_lock.EnterScope())
        {
            // Return a copy of the keys to avoid modification issues
            int count = _keyToFileMap.Keys.Count;
            List<TKey> keys = _keyToFileMap.Keys.ToList();
            _logger.LogDebug("Retrieved {Count} keys from file cache", count);
            return keys;
        }
    }

    /// <summary>
    /// Determines whether the cache is currently available for operations.
    /// </summary>
    /// <returns>
    /// A boolean value indicating the availability of the cache.
    /// Returns <c>true</c> if the cache is available; otherwise, <c>false</c>.
    /// </returns>
   public bool IsAvailable()
   {
       try
       {
           _logger.LogDebug("Checking availability of file cache directory '{DirectoryPath}'", _directoryPath);
   
           // Check if the directory exists
           if (!Directory.Exists(_directoryPath))
           {
               _logger.LogWarning("Cache directory '{DirectoryPath}' does not exist", _directoryPath);
               return false;
           }
   
           // Attempt to create and delete a uniquely named temporary file
           string testFileName = Guid.NewGuid().ToString("N") + ".tmp";
           string testFilePath = Path.Combine(_directoryPath, testFileName);
           _logger.LogDebug("Creating test file '{TestFilePath}' to verify cache availability", testFilePath);
   
           File.WriteAllText(testFilePath, "availability check");
           File.Delete(testFilePath);
   
           _logger.LogDebug("File cache is available at '{DirectoryPath}'", _directoryPath);
           return true;
       }
       catch (Exception ex)
       {
           _logger.LogWarning(ex, "File cache at '{DirectoryPath}' is not available", _directoryPath);
           return false;
       }
   }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key whose associated value is to be retrieved.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, the default value for the type of the value parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// True if the key exists in the cache and the value was successfully retrieved; otherwise, false.
    /// </returns>
    public bool TryGet(TKey key, out TValue? value)
    {
        if (key is null)
        {
            value = default;
            return false;
        }

        return TryGetInternal(key, out value);
    }

    /// <summary>
    /// Stores an item in the file cache with the specified key and value.
    /// </summary>
    /// <param name="key">The key used to identify the cached item. Cannot be null.</param>
    /// <param name="value">The value to be cached associated with the specified key.</param>
    public void Set(TKey key, TValue value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        SetInternal(key, value);
    }

    /// <summary>
    /// Removes the item associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the item to be removed from the cache.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully removed;
    /// otherwise, <see langword="false"/> if the key does not exist or
    /// the removal operation failed.
    /// </returns>
    public bool Remove(TKey key)
    {
        if (key is null)
            return false;

        return RemoveInternal(key);
    }

    /// <summary>
    /// Removes all items from the cache.
    /// </summary>
    /// <remarks>
    /// This method clears all cached items stored in the file cache, leaving it empty.
    /// </remarks>
    public void Clear()
    {
        ClearInternal();
    }
}
