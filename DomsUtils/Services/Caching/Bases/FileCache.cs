using System.Text.Json;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;

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
public class FileCache<TKey, TValue> : CacheBase<TKey, TValue>, ICacheAvailability, ICacheEnumerable<TKey>
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
    /// Represents a serializable key-filename mapping entry
    /// </summary>
    private sealed record KeyMappingEntry(string SerializedKey, string Filename, string KeyTypeName);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCache{TKey,TValue}"/> class
    /// with the specified directory path for file storage.
    /// </summary>
    /// <param name="directoryPath">The directory path where cache files will be stored.</param>
    public FileCache(string directoryPath)
    {
        _directoryPath = directoryPath;
        if (!Directory.Exists(_directoryPath))
            Directory.CreateDirectory(_directoryPath);
            
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
                return;

            try
            {
                var mappings = LoadMappingsFromFile();
                if (mappings != null)
                {
                    _keyToFileMap.Clear();
                    ProcessMappings(mappings);
                }
            }
            catch
            {
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
        var json = File.ReadAllText(_metadataFilePath);
        return JsonSerializer.Deserialize<List<KeyMappingEntry>>(json);
    }

    /// <summary>
    /// Processes a collection of mapping entries and adds valid ones to the key-to-file map.
    /// </summary>
    /// <param name="mappings">The mappings to process.</param>
    private void ProcessMappings(List<KeyMappingEntry> mappings)
    {
        foreach (var mapping in mappings)
        {
            if (IsValidMapping(mapping))
            {
                var key = TryDeserializeKey(mapping.SerializedKey);
                if (key is not null)
                {
                    _keyToFileMap[key] = mapping.Filename;
                }
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
        var filePath = Path.Combine(_directoryPath, mapping.Filename);
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
            var key = JsonSerializer.Deserialize<TKey>(serializedKey);
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
                // Create a serializable version of the mapping
                var serializableMap = new List<KeyMappingEntry>();
                
                foreach (var kvp in _keyToFileMap)
                {
                    try
                    {
                        // Serialize the key as JSON to preserve its structure
                        var serializedKey = JsonSerializer.Serialize(kvp.Key);
                        var keyTypeName = typeof(TKey).FullName ?? throw new InvalidOperationException("Key type name cannot be null");
                        
                        serializableMap.Add(new KeyMappingEntry(serializedKey, kvp.Value, keyTypeName));
                    }
                    catch
                    {
                        // Skip keys that can't be serialized
                    }
                }
                
                var json = JsonSerializer.Serialize(serializableMap);
                File.WriteAllText(_metadataFilePath, json);
            }
            catch
            {
                // Log the error if needed
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
            if (_keyToFileMap.TryGetValue(key, out var existingFilename))
                return existingFilename;
            
            // Otherwise, create a new unique filename
            string filename = Guid.NewGuid().ToString("N") + ".json";
            _keyToFileMap[key] = filename;
            
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
    protected override bool TryGetInternal(TKey key, out TValue value)
    {
        value = default!;

        try
        {
            using (_lock.EnterScope())
            {
                // Check if we have a filename for this key
                if (!_keyToFileMap.TryGetValue(key, out var filename))
                    return false;
                
                var filePath = Path.Combine(_directoryPath, filename);
                
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    value = JsonSerializer.Deserialize<TValue>(json) ?? throw new InvalidOperationException();
                    return true;
                }
                else
                {
                    // File was deleted outside of our control
                    _keyToFileMap.Remove(key);
                    SaveKeyMappings();
                    return false;
                }
            }
        }
        catch
        {
            // Log exception if needed
            return false;
        }
    }

    /// <summary>
    /// Stores a key-value pair in the cache, handling the internal implementation logic for persisting data.
    /// </summary>
    /// <param name="key">The key associated with the value being stored in the cache.</param>
    /// <param name="value">The value to be stored in the cache.</param>
    protected override void SetInternal(TKey key, TValue value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        
        using (_lock.EnterScope())
        {
            try
            {
                // Get or create a filename for this key
                var filename = GetOrCreateFilename(key);
                var filePath = Path.Combine(_directoryPath, filename);
                
                // Serialize and save the value
                var json = JsonSerializer.Serialize(value);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Log exception if needed
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
    protected override bool RemoveInternal(TKey key)
    {
        try
        {
            using (_lock.EnterScope())
            {
                // Check if we have a filename for this key
                if (!_keyToFileMap.TryGetValue(key, out var filename))
                    return false;
                
                var filePath = Path.Combine(_directoryPath, filename);
                
                // Remove the file if it exists
                bool fileRemoved = false;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    fileRemoved = true;
                }
                
                // Remove from our mapping
                _keyToFileMap.Remove(key);
                SaveKeyMappings();
                
                return fileRemoved;
            }
        }
        catch
        {
            // Log exception if needed
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
    protected override void ClearInternal()
    {
        try
        {
            using (_lock.EnterScope())
            {
                // Delete all cache files except the metadata file
                var files = Directory.GetFiles(_directoryPath)
                    .Where(f => Path.GetFileName(f) != Path.GetFileName(_metadataFilePath));
                
                foreach (var file in files)
                    File.Delete(file);
                
                // Clear the key mapping
                _keyToFileMap.Clear();
                SaveKeyMappings();
            }
        }
        catch
        {
            // Log exception if needed
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
        using (_lock.EnterScope())
        {
            // Return a copy of the keys to avoid modification issues
            return _keyToFileMap.Keys.ToList();
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