using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// Amazon S3-backed cache for storing key-value pairs.
/// </summary>
/// <typeparam name="TKey">The key type (must be non-null)</typeparam>
/// <typeparam name="TValue">The value type</typeparam>
public class S3Cache<TKey, TValue> : CacheBase<TKey, TValue>,
    ICacheAvailability,
    ICacheEnumerable<TKey>,
    ICacheEvents<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Represents the name of a storage bucket. This variable is typically used to identify
    /// and interact with a specific storage bucket in cloud services or other storage solutions.
    /// </summary>
    private protected readonly string BucketName;

    /// <summary>
    /// The Amazon S3 client used for interaction with the S3 service,
    /// enabling operations such as storing, retrieving, and managing objects
    /// in the specified S3 bucket.
    /// </summary>
    private protected readonly IAmazonS3 S3Client;

    /// <summary>
    /// Configuration options for JSON serialization and deserialization operations.
    /// Used to customize behavior such as case sensitivity and property naming during
    /// object serialization and deserialization in the Amazon S3-backed cache.
    /// </summary>
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// A function delegate responsible for converting a key of type <typeparamref name="TKey"/> to a string representation.
    /// This string representation is used as the object key within the underlying S3 storage.
    /// </summary>
    private readonly Func<TKey, string> _keyConverter;

    /// <summary>
    /// A function delegate responsible for converting a string representation back to a key of type <typeparamref name="TKey"/>.
    /// This is used to convert S3 object keys back to the original key type during enumeration.
    /// </summary>
    private readonly Func<string, TKey>? _reverseKeyConverter;

    /// <summary>
    /// Logger instance for logging cache operations.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Event triggered after a value has been successfully added or updated in the S3 cache.
    /// </summary>
    public event Action<TKey, TValue> OnSet = delegate { };

    /// <summary>
    /// Amazon S3-backed cache for storing key-value pairs.
    /// </summary>
    /// <typeparam name="TKey">The key type (must be non-null)</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    public S3Cache(
        string bucketName,
        IAmazonS3 s3Client,
        JsonSerializerOptions? serializerOptions = null,
        Func<TKey, string>? keyConverter = null,
        Func<string, TKey>? reverseKeyConverter = null,
        ILogger? logger = null)
    {
        BucketName = !string.IsNullOrWhiteSpace(bucketName)
            ? bucketName
            : throw new ArgumentException("Bucket name cannot be empty.", nameof(bucketName));
        
        S3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        
        _keyConverter = keyConverter ?? (key => key.ToString() 
            ?? throw new InvalidOperationException($"Key {key} converted to null string"));
        
        _reverseKeyConverter = reverseKeyConverter;
        
        _logger = logger ?? NullLogger.Instance;
        
        _logger.LogInformation("Initialized S3Cache with bucket '{BucketName}'", bucketName);
    }

    /// <summary>
    /// Creates a new instance of S3Cache using the specified bucket name and S3 client.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket to use for caching.</param>
    /// <param name="s3Client">The Amazon S3 client instance for interacting with the S3 service.</param>
    /// <returns>A new instance of S3Cache configured with the provided bucket name and S3 client.</returns>
    public static S3Cache<TKey, TValue> Create(string bucketName, IAmazonS3 s3Client)
        => new(bucketName, s3Client);

    /// <summary>
    /// Creates a new S3Cache instance with a custom key converter.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    /// <param name="s3Client">A configured Amazon S3 client instance.</param>
    /// <param name="keyConverter">A function to convert keys to string representations.</param>
    /// <returns>A new instance of S3Cache with the specified key converter.</returns>
    public static S3Cache<TKey, TValue> Create(string bucketName, IAmazonS3 s3Client, Func<TKey, string> keyConverter)
        => new(bucketName, s3Client, keyConverter: keyConverter);

    /// <summary>
    /// Creates a new S3Cache instance with custom key converters.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    /// <param name="s3Client">A configured Amazon S3 client instance.</param>
    /// <param name="keyConverter">A function to convert keys to string representations.</param>
    /// <param name="reverseKeyConverter">A function to convert string representations back to keys.</param>
    /// <returns>A new instance of S3Cache with the specified key converters.</returns>
    public static S3Cache<TKey, TValue> Create(string bucketName, IAmazonS3 s3Client, 
        Func<TKey, string> keyConverter, Func<string, TKey> reverseKeyConverter)
        => new(bucketName, s3Client, keyConverter: keyConverter, reverseKeyConverter: reverseKeyConverter);
        
    /// <summary>
    /// Creates a new S3Cache instance with a logger.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    /// <param name="s3Client">A configured Amazon S3 client instance.</param>
    /// <param name="logger">Logger instance for logging cache operations.</param>
    /// <returns>A new instance of S3Cache with the specified logger.</returns>
    public static S3Cache<TKey, TValue> Create(string bucketName, IAmazonS3 s3Client, ILogger logger)
        => new(bucketName, s3Client, logger: logger);

    /// <summary>
    /// Converts a provided key to a specific format or type.
    /// </summary>
    /// <param name="key">The key to be converted.</param>
    /// <returns>The converted key in the desired format or type.</returns>
    private string ConvertKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        string keyString = _keyConverter(key);
        if (string.IsNullOrEmpty(keyString))
        {
            _logger.LogWarning("Key {Key} converted to empty string", key);
            throw new ArgumentException("Key converted to empty string", nameof(key));
        }
        
        return keyString;
    }

    /// <summary>
    /// Attempts to retrieve a value from the cache based on the provided key.
    /// </summary>
    /// <param name="key">The key used to locate the cached value.</param>
    /// <param name="value">The retrieved value, if found in the cache. If not, the default value for the type is returned.</param>
    /// <returns>True if the value is successfully retrieved; otherwise, false.</returns>
    protected override bool TryGetInternal(TKey key, out TValue value)
    {
        value = default!;

        try
        {
            string keyString = ConvertKey(key);
            _logger.LogDebug("Attempting to get value for key '{Key}' from S3 bucket '{BucketName}'", keyString,
                BucketName);

            GetObjectResponse? response = S3Client.GetObjectAsync(BucketName, keyString).Result;

            using var stream = response.ResponseStream;
            value = JsonSerializer.Deserialize<TValue>(response.ResponseStream, _serializerOptions)!;
            _logger.LogDebug("Successfully retrieved value for key '{Key}' from S3 bucket '{BucketName}'",
                keyString, BucketName);
            return true;
        }
        catch (AggregateException ex) when (ex.InnerException is Amazon.S3.AmazonS3Exception s3Ex && 
            (s3Ex.ErrorCode == "NoSuchKey" || s3Ex.Message.Contains("Key not found")))
        {
            // Expected behavior for missing keys - don't log as error
            _logger.LogDebug("Key '{Key}' not found in S3 bucket '{BucketName}'", key, BucketName);
            return false;
        }
        catch (Amazon.S3.AmazonS3Exception ex) when 
            (ex.ErrorCode == "NoSuchKey" || ex.Message.Contains("Key not found"))
        {
            // Expected behavior for missing keys - don't log as error
            _logger.LogDebug("Key '{Key}' not found in S3 bucket '{BucketName}'", key, BucketName);
            return false;
        }
        catch (Exception ex)
        {
            // Unexpected errors should still be logged
            _logger.LogError(ex, "Unexpected error retrieving key '{Key}' from S3 bucket '{BucketName}'", key,
                BucketName);
            return false;
        }
    }

    /// <summary>
    /// Stores a value in the cache with the specified key.
    /// </summary>
    /// <param name="key">The key associated with the value to store. Must not be null.</param>
    /// <param name="value">The value to store in the cache.</param>
    protected override void SetInternal(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            string keyString = ConvertKey(key);
            _logger.LogDebug("Setting value for key '{Key}' in S3 bucket '{BucketName}'", keyString, BucketName);
            
            string json = JsonSerializer.Serialize(value, _serializerOptions);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using MemoryStream stream = new MemoryStream(bytes);
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = keyString,
                InputStream = stream,
                UseChunkEncoding = false,
                ContentType = "application/json"
            };

            S3Client.PutObjectAsync(request).Wait();
            _logger.LogInformation("Successfully set value for key '{Key}' in S3 bucket '{BucketName}'", keyString, BucketName);
            OnSet(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key '{Key}' in S3 bucket '{BucketName}'", key, BucketName);
            // ignored for backward compatibility
        }
    }

    /// <summary>
    /// Removes an item from the S3 cache using the specified key.
    /// </summary>
    /// <param name="key">The key of the item to be removed from the cache.</param>
    /// <returns>
    /// True if the item was successfully removed, or false if the key is null,
    /// the item does not exist, or an exception occurred during deletion.
    /// </returns>
    protected override bool RemoveInternal(TKey key)
    {
        try
        {
            string keyString = ConvertKey(key);
            _logger.LogDebug("Removing value for key '{Key}' from S3 bucket '{BucketName}'", keyString, BucketName);
            
            S3Client.DeleteObjectAsync(BucketName, keyString).Wait();
            _logger.LogInformation("Successfully removed value for key '{Key}' from S3 bucket '{BucketName}'", keyString, BucketName);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Key '{Key}' not found for removal in S3 bucket '{BucketName}'", key, BucketName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value for key '{Key}' from S3 bucket '{BucketName}'", key, BucketName);
            return false;
        }
    }

    /// <summary>
    /// Removes all cache entries from the S3 bucket associated with this cache instance.
    /// </summary>
    /// <remarks>
    /// This method iterates through all objects in the associated S3 bucket
    /// and deletes them in batches, ensuring that the bucket is cleared completely.
    /// It employs paginated requests to handle large datasets.
    /// </remarks>
    protected override void ClearInternal()
    {
        _logger.LogWarning("Clearing all objects from S3 bucket '{BucketName}'", BucketName);
        
        try {
            ListObjectsV2Request request = new ListObjectsV2Request { BucketName = BucketName, MaxKeys = 1000 };
            int totalDeleted = 0;
            
            do
            {
                ListObjectsV2Response? response = S3Client.ListObjectsV2Async(request).Result;
                int batchCount = response.S3Objects.Count;
                
                if (batchCount > 0)
                {
                    DeleteObjectsRequest deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = BucketName,
                        Objects = response.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }).ToList()
                    };

                    S3Client.DeleteObjectsAsync(deleteRequest).Wait();
                    totalDeleted += batchCount;
                    _logger.LogDebug("Deleted batch of {Count} objects from S3 bucket '{BucketName}'", batchCount, BucketName);
                }

                request.ContinuationToken = response.NextContinuationToken;
            }
            while (request.ContinuationToken != null);
            
            _logger.LogInformation("Successfully cleared {Count} objects from S3 bucket '{BucketName}'", totalDeleted, BucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing objects from S3 bucket '{BucketName}'", BucketName);
        }
    }

    /// <summary>
    /// Checks whether the S3Cache is available by verifying the accessibility of the S3 bucket.
    /// </summary>
    /// <returns>True if the S3 bucket is accessible, otherwise false.</returns>
    public override bool IsAvailable()
    {
        try
        {
            _logger.LogDebug("Checking availability of S3 bucket '{BucketName}'", BucketName);
            S3Client.GetBucketLocationAsync(BucketName).Wait();
            _logger.LogDebug("S3 bucket '{BucketName}' is available", BucketName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "S3 bucket '{BucketName}' is not available", BucketName);
            return false;
        }
    }

    /// <summary>
    /// Represents a collection of keys used within a system.
    /// </summary>
    public override IEnumerable<TKey> Keys()
    {
        if (_reverseKeyConverter == null)
        {
            _logger.LogWarning("Key enumeration not supported in generic S3Cache<TKey, TValue>. Use string keys or provide reverse key converter.");
            throw new NotSupportedException(
                "Key enumeration requires reverse key mapping. Use string keys or provide reverse key converter.");
        }

        try
        {
            return GetKeysInternal();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error enumerating keys in S3 bucket '{BucketName}'", ex);
        }
    }

    /// <summary>
    /// Retrieves all keys stored in the Amazon S3 bucket associated with this cache.
    /// </summary>
    /// <returns>
    /// A collection of keys representing the stored items in the S3 bucket.
    /// </returns>
    private IEnumerable<TKey> GetKeysInternal()
    {
        ListObjectsV2Request request = new ListObjectsV2Request { BucketName = BucketName, MaxKeys = 1000 };
        
        do
        {
            ListObjectsV2Response? response = S3Client.ListObjectsV2Async(request).Result;

            if (response?.S3Objects is null)
            {
                _logger.LogWarning($"Response was null or S3Objects was null. Continuing with next page. Code: {response?.HttpStatusCode}");
                continue;
            }
            
            foreach (S3Object? obj in response.S3Objects)
            {
                if (obj.Key is null)
                    continue;
                
                yield return _reverseKeyConverter!(obj.Key);
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);
    }
}

/// <summary>
/// Amazon S3-backed cache for storing key-value pairs.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
public class S3Cache<TValue> : S3Cache<string, TValue>
{
    /// <summary>
    /// Represents a cache implementation using an Amazon S3 bucket as the storage backend.
    /// </summary>
    /// <param name="bucketName">The name of the Amazon S3 bucket used for caching.</param>
    /// <param name="s3Client">The Amazon S3 client configured for communication with the S3 service.</param>
    /// <param name="serializerOptions">Optional parameters for customizing JSON serialization behavior.</param>
    /// <param name="logger">Optional logger for logging cache operations.</param>
    public S3Cache(string bucketName, IAmazonS3 s3Client, JsonSerializerOptions? serializerOptions = null, ILogger? logger = null)
        : base(bucketName, s3Client, serializerOptions, logger: logger)
    {
    }

    /// <summary>
    /// Creates a new S3Cache instance with the specified bucket name and S3 client.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket to be used for caching.</param>
    /// <param name="s3Client">The Amazon S3 client instance to interact with the S3 bucket.</param>
    /// <returns>A new instance of S3Cache with the specified configuration.</returns>
    public new static S3Cache<TValue> Create(string bucketName, IAmazonS3 s3Client)
        => new(bucketName, s3Client);
        
    /// <summary>
    /// Creates a new S3Cache instance with a logger.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    /// <param name="s3Client">A configured Amazon S3 client instance.</param>
    /// <param name="logger">Logger instance for logging cache operations.</param>
    /// <returns>A new instance of S3Cache with the specified logger.</returns>
    public static S3Cache<TValue> Create(string bucketName, IAmazonS3 s3Client, ILogger logger)
        => new(bucketName, s3Client, logger: logger);

    /// <summary>
    /// Retrieves a collection of keys currently stored in the Amazon S3-backed cache.
    /// </summary>
    /// <returns>
    /// An enumerable of keys representing the stored items in the cache.
    /// </returns>
    public new IEnumerable<string> Keys()
    {
        try
        {
            return GetKeysInternal();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error enumerating keys in S3 bucket '{BucketName}'", ex);
        }
    }

    /// <summary>
    /// Retrieves all keys stored in the Amazon S3 bucket associated with this cache.
    /// </summary>
    /// <returns>
    /// A collection of strings representing the keys stored in the S3 bucket.
    /// </returns>
    private IEnumerable<string> GetKeysInternal()
    {
        ListObjectsV2Request request = new ListObjectsV2Request { BucketName = BucketName, MaxKeys = 1000 };
        
        do
        {
            ListObjectsV2Response? response = S3Client.ListObjectsV2Async(request).Result;
            
            foreach (S3Object? obj in response.S3Objects)
                yield return obj.Key;

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);
    }
}