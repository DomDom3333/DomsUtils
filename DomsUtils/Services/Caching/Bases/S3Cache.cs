using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Bases;

/// <summary>
/// Represents an Amazon S3-backed caching mechanism that extends the CacheBase class
/// and implements several interfaces to provide advanced caching functionality.
/// </summary>
/// <typeparam name="TValue">
/// The type of the value to be cached.
/// </typeparam>
/// <remarks>
/// This class utilizes S3 as the backing store for the cache. It includes methods
/// for retrieving, setting, removing keys/values, and clearing the cache. It also
/// supports event-driven behavior via the <c>OnSet</c> event, and provides features
/// such as key enumeration and availability checks.
/// </remarks>
/// <implements>
/// ICacheAvailability
/// ICacheEnumerable
/// ICacheEvents
/// </implements>
/// <events>
/// <c>OnSet</c> - Occurs when a key-value pair is added or updated in the cache.
/// </events>
/// <example>
/// This class is intended for use in scenarios where S3 is used to store and manage cache data.
/// </example>
public class S3Cache<TValue> : CacheBase<string, TValue>, 
    ICacheAvailability, 
    ICacheEnumerable<string>, 
    ICacheEvents<string, TValue>
{
    /// <summary>
    /// Stores the name of the S3 bucket used for caching operations.
    /// </summary>
    /// <remarks>
    /// The bucket name is provided during the initialization of the <c>S3Cache</c> instance and
    /// is validated to ensure it is non‐empty. It is used for all interactions with the
    /// Amazon S3 client, including retrieving, storing, and deleting cached items.
    /// </remarks>
    private readonly string _bucketName;

    /// <summary>
    /// Represents the Amazon S3 client used for interacting with the S3 service.
    /// This instance facilitates operations such as retrieving, uploading, removing,
    /// and listing objects within the Amazon S3 bucket specified by the cache.
    /// </summary>
    private readonly IAmazonS3 _s3Client;

    /// <summary>
    /// Defines serialization options used by the <see cref="S3Cache{TValue}"/> class
    /// for converting objects to and from JSON format.
    /// </summary>
    /// <remarks>
    /// These options are utilized by the underlying <see cref="System.Text.Json.JsonSerializer"/>
    /// for configuring serialization behavior, such as property naming rules and case sensitivity.
    /// </remarks>
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Event triggered when a value is successfully set in the cache.
    /// </summary>
    /// <remarks>
    /// The <c>OnSet</c> event occurs after the value has been successfully saved to the underlying storage.
    /// It provides the key and the value that were set in the cache as event parameters.
    /// This event is particularly useful for handling post-save operations, such as logging, monitoring, or notifying other components.
    /// </remarks>
    /// <typeparamref name="string"/>: The key associated with the cached value.
    /// <typeparamref name="TValue"/>: The cached value being set.
    /// <seealso cref="SetInternal(string, TValue)"/>
    public event Action<string, TValue> OnSet = delegate { };

    /// <summary>
    /// Provides a cache implementation that stores and retrieves objects using Amazon S3 as the backend.
    /// The caller must supply the following dependencies:
    /// • bucketName: Name of the S3 bucket used for storage. Must be a non-empty string.
    /// • s3Client:   Pre-configured instance of <see cref="IAmazonS3"/> for S3 API communication.
    /// Throws <see cref="ArgumentNullException"/> if null.
    /// • serializerOptions: Optional JSON serializer settings used during serialization/deserialization.
    /// If omitted, defaults to settings with property name case insensitivity.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of the values to be stored in the cache. This type should be serializable to JSON.
    /// </typeparam>
    public S3Cache(
        string bucketName,
        IAmazonS3 s3Client,
        JsonSerializerOptions? serializerOptions = null)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name must be non‐empty.", nameof(bucketName));

        _bucketName = bucketName;
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _serializerOptions = serializerOptions 
                             ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key
    /// from the S3 storage. If the key does not exist or an error occurs,
    /// the method returns false and outputs the default value for the type.
    /// </summary>
    /// <param name="key">The key of the item to retrieve. Must not be null or empty.</param>
    /// <param name="value">The value retrieved from S3, if found. If the operation fails, this will contain the default value for the type.</param>
    /// <returns>True if the item was successfully retrieved; otherwise, false.</returns>
    protected override bool TryGetInternal(string key, out TValue value)
    {
        value = default!;
        if (string.IsNullOrEmpty(key)) return false;

        try
        {
            var response = _s3Client
                .GetObjectAsync(new GetObjectRequest { BucketName = _bucketName, Key = key })
                .GetAwaiter()
                .GetResult(); // keeping synchronous for CacheBase

            using (response.ResponseStream)
            {
                // Deserialize directly from the response stream
                value = JsonSerializer.Deserialize<TValue>(
                    response.ResponseStream, 
                    _serializerOptions
                )!;
            }

            return true;
        }
        catch (AmazonS3Exception s3Ex) when (s3Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Key doesn’t exist
            return false;
        }
        catch
        {
            // If you have a logging framework, log the exception here.
            return false;
        }
    }

    /// <summary>
    /// Serializes the specified <paramref name="value"/> to JSON and uploads it to an S3 bucket under the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">A non-empty string specifying the object key in the S3 bucket. Throws <see cref="ArgumentException"/> if null or empty.</param>
    /// <param name="value">The value to be serialized and uploaded to S3. Cannot be null.</param>
    protected override void SetInternal(string key, TValue value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key must be non‐empty.", nameof(key));

        // Serialize the value to a UTF‐8 byte array
        var json = JsonSerializer.Serialize(value, _serializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var ms = new MemoryStream(bytes);
        var request = new PutObjectRequest
        {
            BucketName  = _bucketName,
            Key         = key,
            InputStream = ms,
            ContentType = "application/json"
        };

        try
        {
            _s3Client
                .PutObjectAsync(request)
                .GetAwaiter()
                .GetResult();
                
            // Fire the event *after* a successful upload
            OnSet?.Invoke(key, value);
        }
        catch
        {
            // Swallow or log the exception so a failing S3 write doesn’t kill the app.
            // If you have a logger, call logger.LogError(...) here.
        }
    }

    /// <summary>
    /// Removes an object from the S3 bucket using the provided key.
    /// Returns <c>true</c> if the object was successfully removed or found;
    /// returns <c>false</c> if the key was not found or removal failed.
    /// </summary>
    /// <param name="key">The key of the object to be removed from the S3 bucket. Must not be null or empty.</param>
    /// <returns>Returns <c>true</c> if the object existed and was removed successfully, or <c>false</c> otherwise.</returns>
    protected override bool RemoveInternal(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        try
        {
            _s3Client
                .DeleteObjectAsync(_bucketName, key)
                .GetAwaiter()
                .GetResult();
                
            return true;
        }
        catch (AmazonS3Exception s3Ex) when (s3Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Key didn’t exist
            return false;
        }
        catch
        {
            // Log or ignore
            return false;
        }
    }

    /// <summary>
    /// Clears all objects from the cache by using a batching mechanism to delete chunks of up to 1000 items at a time.
    /// Utilizes the DeleteObjects API for efficient bulk deletion of keys.
    /// This operation is internal to ensure controlled clearing of the associated S3 bucket contents.
    /// </summary>
    protected override void ClearInternal()
    {
        const int batchSize = 1000;
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            MaxKeys    = batchSize
        };

        ListObjectsV2Response listResponse;
        do
        {
            listResponse = _s3Client
                .ListObjectsV2Async(listRequest)
                .GetAwaiter()
                .GetResult();

            if (listResponse.S3Objects.Count > 0)
            {
                // Build a batch delete request
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _bucketName,
                    Objects = listResponse.S3Objects
                        .Select(o => new KeyVersion { Key = o.Key })
                        .ToList()
                };

                try
                {
                    _s3Client
                        .DeleteObjectsAsync(deleteRequest)
                        .GetAwaiter()
                        .GetResult();
                }
                catch
                {
                }
            }

            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        }
        while (listResponse.IsTruncated == true);
    }

    /// <summary>
    /// Checks whether the S3Cache is available by attempting to access the S3 bucket.
    /// If the bucket is accessible, the cache is considered available; otherwise, it is not.
    /// </summary>
    /// <returns>
    /// A boolean value indicating the availability of the S3Cache:
    /// true if the cache is available; false otherwise.
    /// </returns>
    public override bool IsAvailable()
    {
        try
        {
            _s3Client
                .GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = _bucketName })
                .GetAwaiter()
                .GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves all keys currently stored in the associated S3 bucket by paging through the bucket contents.
    /// </summary>
    /// <returns>Enumerable collection of all keys present in the S3 bucket.</returns>
    public IEnumerable<string> Keys()
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            MaxKeys    = 1000
        };

        ListObjectsV2Response listResponse;
        do
        {
            listResponse = _s3Client
                .ListObjectsV2Async(listRequest)
                .GetAwaiter()
                .GetResult();

            foreach (var obj in listResponse.S3Objects)
                yield return obj.Key;

            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        }
        while (listResponse.IsTruncated == true);
    }
}