using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;

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
        Func<TKey, string>? keyConverter = null)
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
    /// Converts a provided key to a specific format or type.
    /// </summary>
    /// <param name="key">The key to be converted.</param>
    /// <returns>The converted key in the desired format or type.</returns>
    private string ConvertKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        var keyString = _keyConverter(key);
        return !string.IsNullOrEmpty(keyString) 
            ? keyString 
            : throw new ArgumentException("Key converted to empty string", nameof(key));
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
            var keyString = ConvertKey(key);
            var response = S3Client.GetObjectAsync(BucketName, keyString).Result;

            using (response.ResponseStream)
            {
                value = JsonSerializer.Deserialize<TValue>(response.ResponseStream, _serializerOptions)!;
                return true;
            }
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch
        {
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
            var keyString = ConvertKey(key);
            var json = JsonSerializer.Serialize(value, _serializerOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var stream = new MemoryStream(bytes);
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = keyString,
                InputStream = stream,
                ContentType = "application/json"
            };

            S3Client.PutObjectAsync(request).Wait();
            OnSet(key, value);
        }
        catch
        {
            // ignored
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
            var keyString = ConvertKey(key);
            S3Client.DeleteObjectAsync(BucketName, keyString).Wait();
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch
        {
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
        var request = new ListObjectsV2Request { BucketName = BucketName, MaxKeys = 1000 };
        
        do
        {
            var response = S3Client.ListObjectsV2Async(request).Result;
            
            if (response.S3Objects.Count > 0)
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = BucketName,
                    Objects = response.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }).ToList()
                };

                S3Client.DeleteObjectsAsync(deleteRequest).Wait();
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);
    }

    /// <summary>
    /// Checks whether the S3Cache is available by verifying the accessibility of the S3 bucket.
    /// </summary>
    /// <returns>True if the S3 bucket is accessible, otherwise false.</returns>
    public override bool IsAvailable()
    {
        try
        {
            S3Client.GetBucketLocationAsync(BucketName).Wait();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Represents a collection of keys used within a system.
    /// </summary>
    public IEnumerable<TKey> Keys()
    {
        throw new NotSupportedException(
            "Key enumeration requires reverse key mapping. Use string keys or implement custom mapping.");
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
    public S3Cache(string bucketName, IAmazonS3 s3Client, JsonSerializerOptions? serializerOptions = null)
        : base(bucketName, s3Client, serializerOptions)
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
    /// Represents a collection of keys used for accessing specific resources or data.
    /// </summary>
    public new IEnumerable<string> Keys()
    {
        var request = new ListObjectsV2Request { BucketName = BucketName, MaxKeys = 1000 };
        
        do
        {
            var response = S3Client.ListObjectsV2Async(request).Result;
            
            foreach (var obj in response.S3Objects)
                yield return obj.Key;

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);
    }
}