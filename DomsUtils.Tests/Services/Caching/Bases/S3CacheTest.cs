using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using DomsUtils.Services.Caching.Bases;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DomsUtils.Tests.Services.Caching.Bases
{
    /// <summary>
    /// A test class containing unit tests for S3Cache and its related operations.
    /// This class validates different scenarios for S3Cache, ensuring proper functionality
    /// such as creation, exceptions, and operations like getting, setting, removing, clearing,
    /// and checking availability of cache objects in an S3 bucket.
    /// </summary>
    [TestClass]
    public class S3CacheTest
    {
        /// <summary>
        /// An object representing a mocked implementation of an S3 client.
        /// This mock object can be used for unit testing to simulate interactions
        /// with an S3 storage service without requiring a real connection to AWS S3.
        /// </summary>
        private Mock<IAmazonS3> _mockS3Client;

        /// <summary>
        /// Represents the name of the test bucket used for unit tests related to
        /// S3 caching functionality.
        /// </summary>
        private const string TestBucketName = "test-bucket";

        /// <summary>
        /// Represents a predefined constant key used for testing purposes.
        /// </summary>
        private const string TestKeyConstant = "test-key"; // Renamed to avoid conflict

        /// <summary>
        /// Represents a constant value used as a test string in unit tests.
        /// </summary>
        private const string TestValue = "test-value";

        /// Sets up the necessary resources and dependencies for the test environment.
        /// This method is executed before each test method in the test class, ensuring that
        /// a fresh and isolated instance of dependencies is prepared. In this case, it initializes
        /// a mocked instance of the IAmazonS3 interface to simulate interactions with AWS S3
        /// without relying on actual S3 infrastructure.
        /// Usage of this method ensures consistency and test isolation for all related test cases.
        [TestInitialize]
        public void Setup()
        {
            _mockS3Client = new Mock<IAmazonS3>();
        }

        /// Verifies that the constructor of the `S3Cache<TValue>` class initializes an instance correctly when provided with valid parameters.
        /// This test ensures that:
        /// - An instance of the `S3Cache<TValue>` class can be created without throwing exceptions.
        /// - The created instance is not null, confirming proper initialization.
        /// It tests the behavior of the `S3Cache<TValue>` constructor with valid inputs, including a valid bucket name
        /// and a properly mocked instance of `IAmazonS3`.
        [TestMethod]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Test implementation
            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            Assert.IsNotNull(cache);
        }

        /// Tests the behavior of the S3Cache constructor when an empty bucket name is provided.
        /// Validates that an ArgumentException is thrown when an empty string is passed as the bucket name.
        /// Ensures that the constructor enforces required preconditions for proper initialization.
        /// This test is critical for verifying input validation in S3Cache constructor logic.
        [TestMethod]
        public void Constructor_EmptyBucketName_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                new S3Cache<string>("", _mockS3Client.Object));
        }

        /// <summary>
        /// Tests the constructor of the <see cref="S3Cache{TValue}"/> class to ensure it throws
        /// an <see cref="ArgumentException"/> when the provided bucket name contains only whitespace characters.
        /// </summary>
        /// <remarks>
        /// A bucket name that consists solely of whitespace characters is invalid and should not
        /// allow the <see cref="S3Cache{TValue}"/> to be constructed.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the bucket name provided to the constructor is composed entirely of whitespace.
        /// </exception>
        [TestMethod]
        public void Constructor_WhitespaceBucketName_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                new S3Cache<string>("   ", _mockS3Client.Object));
        }

        /// <summary>
        /// Verifies that the constructor of <see cref="S3Cache{TValue}"/> throws an <see cref="ArgumentNullException"/>
        /// when a null <see cref="IAmazonS3"/> client is provided.
        /// </summary>
        /// <remarks>
        /// This test ensures that the <see cref="S3Cache{TValue}"/> class validates its inputs and guarantees that
        /// a valid <see cref="IAmazonS3"/> implementation is supplied during object instantiation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <see cref="IAmazonS3"/> client parameter is null.
        /// </exception>
        [TestMethod]
        public void Constructor_NullS3Client_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new S3Cache<string>(TestBucketName, null));
        }

        /// Tests that the S3Cache instance is correctly initialized when custom JsonSerializerOptions are provided.
        /// This method validates that creating an instance of S3Cache with a non-null JsonSerializerOptions object
        /// initializes the cache without errors. The resulting instance is verified to be non-null to ensure
        /// proper initialization.
        [TestMethod]
        public void Constructor_WithCustomOptions_InitializesCorrectly()
        {
            var options = new JsonSerializerOptions();
            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object, options);
            Assert.IsNotNull(cache);
        }

        /// <summary>
        /// Verifies that an instance of <see cref="S3Cache{TKey, TValue}"/> is created successfully using basic parameters.
        /// </summary>
        /// <remarks>
        /// This test ensures that the <see cref="S3Cache{TKey, TValue}.Create"/> method creates a non-null instance
        /// when minimal required parameters, such as <paramref name="bucketName"/> and <paramref name="s3Client"/>, are provided.
        /// </remarks>
        /// <exception cref="AssertFailedException">
        /// Thrown if the created instance is null, indicating that the creation process failed.
        /// </exception>
        [TestMethod]
        public void Create_BasicParameters_CreatesInstance()
        {
            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object);
            Assert.IsNotNull(cache);
        }

        /// Tests that the `S3Cache` instance is successfully created when a key converter function is provided.
        /// This method verifies if the `S3Cache<TKey, TValue>` object is correctly instantiated using the `Create` method
        /// with a specified key converter function and valid bucket name and Amazon S3 client.
        /// Preconditions:
        /// - A valid bucket name is provided.
        /// - An `IAmazonS3` implementation is mocked and initialized.
        /// - A valid key converter function is supplied that converts a key of type `TKey` to a string.
        /// Postconditions:
        /// - An `S3Cache<TKey, TValue>` instance is created successfully.
        /// - The instance is not null after creation, ensuring successful initialization.
        /// Exceptions:
        /// No exceptions are expected during this test's execution if valid inputs are provided.
        [TestMethod]
        public void Create_WithKeyConverter_CreatesInstance()
        {
            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object, k => k.Value);
            Assert.IsNotNull(cache);
        }

        /// Verifies that an instance of the S3Cache class is created successfully when both
        /// a key converter function and a reverse key converter function are provided.
        /// This test validates the behavior of the `Create` method in the `S3Cache` class when
        /// it is configured with custom key conversion logic for transforming a key type to
        /// a string representation and vice versa. The test ensures the cache is not null
        /// after creation, indicating successful instantiation.
        /// Preconditions:
        /// - A valid bucket name must be supplied.
        /// - A valid S3 client mock must be initialized.
        /// - Custom key converter and reverse key converter functions must be provided.
        /// Postconditions:
        /// - An instance of `S3Cache` is created and verified to be non-null.
        [TestMethod]
        public void Create_WithBothConverters_CreatesInstance()
        {
            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object,
                k => k.Value, s => new TestKey { Value = s });
            Assert.IsNotNull(cache);
        }

        /// <summary>
        /// Initializes a new instance of the class, associating it with the provided logger.
        /// </summary>
        /// <param name="logger">
        /// An instance of a logger to be used for logging operations during the execution of the created object.
        /// </param>
        /// <returns>
        /// A new instance of the object configured with the provided logger.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the logger parameter is null.
        /// </exception>
        [TestMethod]
        public void Create_WithLogger_CreatesInstance()
        {
            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger>());
            Assert.IsNotNull(cache);
        }

        /// Tests the behavior of the TryGet method when a key exists in the S3 cache.
        /// This method validates the following expectations:
        /// - The TryGet method should return true if the specified key exists in the S3 cache.
        /// - The output value should match the expected value retrieved from the S3 bucket.
        /// The test sets up a mocked S3 client to simulate the behavior of an S3 service, ensuring that a response
        /// with the correct key and value is returned. A stream containing serialized test data is used as the response.
        /// The test asserts that the method:
        /// - Returns true when the key exists.
        /// - Outputs the expected value corresponding to the key.
        [TestMethod]
        public void TryGet_ExistingKey_ReturnsTrue()
        {
            using var responseStream = new MemoryStream(Encoding.UTF8.GetBytes("\"test-value\""));
            GetObjectResponse response = new GetObjectResponse
            {
                ResponseStream = responseStream,
                HttpStatusCode = System.Net.HttpStatusCode.OK
            };

            // Mock the string overload that's actually being called
            _mockS3Client.Setup(x => x.GetObjectAsync(TestBucketName, TestKeyConstant, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.TryGet(TestKeyConstant, out string value);

            Assert.IsTrue(result);
            Assert.AreEqual(TestValue, value);
        }

        /// <summary>
        /// Verifies the behavior of attempting to retrieve a value
        /// using a key that does not exist in the collection or dictionary.
        /// This method tests the TryGet pattern and ensures that when a non-existing key
        /// is provided, the correct response (false) is returned, and no value is set.
        /// </summary>
        [TestMethod]
        public void TryGet_NonExistingKey_ReturnsFalse()
        {
            var s3Exception = new AmazonS3Exception("The specified key does not exist.")
            {
                ErrorCode = "NoSuchKey"
            };

            _mockS3Client.Setup(x => x.GetObjectAsync(It.Is<GetObjectRequest>(req =>
                    req.BucketName == TestBucketName && req.Key == TestKeyConstant), It.IsAny<CancellationToken>()))
                .ThrowsAsync(s3Exception);

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.TryGet(TestKeyConstant, out string value);

            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        /// Tests the behavior of the TryGet method when an AggregateException containing an AmazonS3Exception
        /// with the "NoSuchKey" error code is thrown during the S3 GetObjectAsync operation.
        /// This test simulates an AggregateException containing an AmazonS3Exception with the "NoSuchKey" error code,
        /// which corresponds to the scenario where the specified key does not exist in the S3 bucket.
        /// It verifies that the TryGet method returns false and outputs a null value in such a case.
        /// Preconditions:
        /// The mock S3 client is configured to throw an AggregateException with an AmazonS3Exception indicating
        /// a "NoSuchKey" error on a GetObjectAsync call.
        /// Expected Outcome:
        /// The TryGet method should return false, and the output value should be null.
        [TestMethod]
        public void TryGet_AggregateExceptionWithNoSuchKey_ReturnsFalse()
        {
            var s3Exception = new AmazonS3Exception("The specified key does not exist.")
            {
                ErrorCode = "NoSuchKey"
            };
            var aggregateException = new AggregateException(s3Exception);

            _mockS3Client.Setup(x => x.GetObjectAsync(It.Is<GetObjectRequest>(req =>
                    req.BucketName == TestBucketName && req.Key == TestKeyConstant), It.IsAny<CancellationToken>()))
                .ThrowsAsync(aggregateException);

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.TryGet(TestKeyConstant, out string value);

            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        /// Tests the `TryGet` method of the `S3Cache` class to ensure that it returns `false` when an unexpected exception
        /// is thrown by the underlying Amazon S3 client during the retrieval process.
        /// This test simulates an unexpected exception, such as `InvalidOperationException`, that is not related to a
        /// missing key or other expected conditions. It verifies that the method gracefully handles the exception
        /// without crashing, returns `false`, and ensures that the out parameter `value` is set to `null`.
        [TestMethod]
        public void TryGet_UnexpectedException_ReturnsFalse()
        {
            _mockS3Client.Setup(x => x.GetObjectAsync(It.Is<GetObjectRequest>(req =>
                    req.BucketName == TestBucketName && req.Key == TestKeyConstant), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.TryGet(TestKeyConstant, out string value);

            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        /// <summary>
        /// Validates the behavior of the <see cref="S3Cache{TKey, TValue}.TryGet"/> method when invoked with a null key.
        /// </summary>
        /// <remarks>
        /// Ensures that the method throws an <see cref="ArgumentNullException"/> if the provided key is null.
        /// This test verifies the proper handling of invalid input by the cache implementation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the key argument passed to <see cref="S3Cache{TKey, TValue}.TryGet"/> is null.
        /// </exception>
        [TestMethod]
        public void TryGet_NullKey_ThrowsArgumentNullException()
        {
            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            Assert.ThrowsException<ArgumentNullException>(() =>
                cache.TryGet(null, out string value));
        }

        /// <summary>
        /// Tests the behavior of the TryGet method when the provided key converter returns null.
        /// Ensures that the method throws an <see cref="InvalidOperationException"/> in this scenario.
        /// </summary>
        /// <remarks>
        /// The test validates that the cache handles the case where a key converter returns null for a given key.
        /// This situation is deemed invalid, and the implementation is expected to throw an exception
        /// to prevent undefined behavior when attempting to access the cache.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the key converter returns null, which is not a valid key representation for the cache.
        /// </exception>
        [TestMethod]
        public void TryGet_KeyConverterReturnsNull_ThrowsInvalidOperationException()
        {
            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object, k => null);
            var testKey = new TestKey { Value = "test" };

            var result = cache.TryGet(testKey, out string value);
    
            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        /// Verifies that the `TryGet` method throws an `ArgumentException` when the key converter function
        /// returns an empty string. This test ensures that invalid key conversions are not allowed, and
        /// appropriate exception handling is in place.
        /// The method tests the following conditions:
        /// - A cache instance is configured with a key converter function that returns an empty string.
        /// - The `TryGet` method is called with a valid key.
        /// - The expected behavior is that `TryGet` throws an `ArgumentException`.
        [TestMethod]
        public void TryGet_KeyConverterReturnsEmptyString_ThrowsArgumentException()
        {
            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object, k => "");
            var testKey = new TestKey { Value = "test" };

            var result = cache.TryGet(testKey, out string value);
    
            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        /// <summary>
        /// Tests that the <see cref="Set(TKey, TValue)"/> method successfully calls the
        /// <see cref="IAmazonS3.PutObjectAsync(PutObjectRequest, CancellationToken)"/> method when provided
        /// with a valid key and value.
        /// </summary>
        /// <remarks>
        /// This test initializes an instance of <see cref="S3Cache{TValue}"/> with a mocked S3 client.
        /// It ensures that the PutObjectAsync method is invoked with the correct bucket name and key
        /// when setting a value in the cache.
        /// </remarks>
        /// <exception cref="MockException">
        /// Thrown if the <see cref="IAmazonS3.PutObjectAsync(PutObjectRequest, CancellationToken)"/> method
        /// is not called as expected.
        /// </exception>
        [TestMethod]
        public void Set_ValidKeyValue_CallsPutObject()
        {
            _mockS3Client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse());

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            cache.Set(TestKeyConstant, TestValue);

            _mockS3Client.Verify(x => x.PutObjectAsync(It.Is<PutObjectRequest>(req =>
                req.Key == TestKeyConstant &&
                req.BucketName == TestBucketName), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// Tests that providing a null key to the Set method of S3Cache throws an ArgumentNullException.
        /// This test validates that the Set method enforces a non-null constraint on its key parameter and
        /// throws the appropriate exception when a null key is provided. This ensures the integrity of key values in the cache.
        /// Preconditions:
        /// - An instance of S3Cache is created with valid initialization parameters.
        /// Expected Behavior:
        /// - An ArgumentNullException is thrown when the Set method is called with a null key.
        [TestMethod]
        public void Set_NullKey_ThrowsArgumentNullException()
        {
            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            Assert.ThrowsException<ArgumentNullException>(() =>
                cache.Set(null, TestValue));
        }

        /// <summary>
        /// Tests that the Set method of the S3Cache does not throw an exception when an AmazonS3Exception is raised
        /// during the call to PutObjectAsync.
        /// </summary>
        /// <remarks>
        /// This test ensures that the S3Cache gracefully handles exceptions from AWS S3 when attempting to store an object.
        /// The method mocks an AmazonS3Exception to simulate an error during the PutObjectAsync call,
        /// and verifies that it does not propagate the exception to the calling code.
        /// </remarks>
        [TestMethod]
        public void Set_S3Exception_DoesNotThrow()
        {
            _mockS3Client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("Error"));

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            cache.Set(TestKeyConstant, TestValue);
            // Should not throw
        }

        /// Tests the Remove method of the S3Cache class to ensure that it returns true
        /// when attempting to remove an existing key from the cache.
        /// This test verifies that the DeleteObjectAsync method of the IAmazonS3 client
        /// is called exactly once with the correct parameters.
        /// Preconditions:
        /// - The S3Cache instance is initialized with a valid bucket name and a mock IAmazonS3 client.
        /// - The key that is being removed exists in the cache.
        /// Test Steps:
        /// 1. Setup the mock IAmazonS3 client to respond successfully to DeleteObjectAsync.
        /// 2. Create an instance of S3Cache using the mock IAmazonS3 client.
        /// 3. Call the Remove method with an existing key.
        /// 4. Verify that the Remove method returns true.
        /// 5. Ensure that DeleteObjectAsync on the IAmazonS3 client is invoked exactly once with the proper arguments.
        /// Postconditions:
        /// - The cache indicates successful removal of the specified key.
        /// - The DeleteObjectAsync method of the IAmazonS3 client is invoked with the correct arguments.
        [TestMethod]
        public void Remove_ExistingKey_ReturnsTrue()
        {
            _mockS3Client.Setup(x => x.DeleteObjectAsync(TestBucketName, TestKeyConstant, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectResponse());

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.Remove(TestKeyConstant);

            Assert.IsTrue(result);
            _mockS3Client.Verify(x => x.DeleteObjectAsync(TestBucketName, TestKeyConstant, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// Tests the behavior of the Remove method when attempting to remove a non-existing key.
        /// This test ensures that the Remove method returns false when the specified key does not exist
        /// in the S3 bucket. It verifies that the appropriate exception is handled correctly and that the
        /// method does not incorrectly indicate that a non-existing key was removed.
        /// The test setup includes mocking an S3 client to simulate a scenario where a delete operation
        /// for a non-existing key returns an AmazonS3Exception with an error code "NoSuchKey". This allows
        /// for the validation of the method's behavior under such conditions.
        /// Validates:
        /// - The method returns false when the specified key does not exist.
        /// - The DeleteObjectAsync method of the mocked S3 client is called exactly once.
        [TestMethod]
        public void Remove_NonExistingKey_ReturnsFalse()
        {
            var s3Exception = new AmazonS3Exception("The specified key does not exist.")
            {
                ErrorCode = "NoSuchKey",
                StatusCode = System.Net.HttpStatusCode.NotFound
            };

            _mockS3Client.Setup(x => x.DeleteObjectAsync(TestBucketName, TestKeyConstant, It.IsAny<CancellationToken>()))
                .ThrowsAsync(s3Exception);

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.Remove(TestKeyConstant);

            Assert.IsFalse(result);
            
            // Verify the method was actually called
            _mockS3Client.Verify(x => x.DeleteObjectAsync(TestBucketName, TestKeyConstant, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Handles unexpected exceptions encountered during execution and ensures proper return behavior.
        /// </summary>
        /// <returns>
        /// A boolean value indicating the success or failure of the operation. Returns <c>false</c> in case of unexpected exceptions.
        /// </returns>
        /// <remarks>
        /// This method is designed to handle and encapsulate unexpected exceptions gracefully.
        /// It ensures that the system remains stable and predictable by returning <c>false</c> when exceptions occur.
        /// Developers are encouraged to review and log any unexpected exceptions for debugging and maintenance purposes.
        /// </remarks>
        [TestMethod]
        public void Remove_UnexpectedException_ReturnsFalse()
        {
            _mockS3Client.Setup(x => x.DeleteObjectAsync(It.Is<DeleteObjectRequest>(req =>
                    req.BucketName == TestBucketName && req.Key == TestKeyConstant), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.Remove(TestKeyConstant);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Deletes all objects within the current context or collection.
        /// This method is intended to clear any stored objects, effectively resetting
        /// the state of the context by removing all associated instances.
        /// </summary>
        /// <remarks>
        /// Use this method with caution, as it will remove all existing objects
        /// and cannot be undone. Ensure any necessary data has been backed up or processed
        /// before invoking this method. Primarily intended for scenarios where a complete
        /// reset or cleanup is required.
        /// </remarks>
        [TestMethod]
        public void Clear_WithObjects_DeletesAllObjects()
        {
            var listResponse = new ListObjectsV2Response
            {
                S3Objects = new List<S3Object>
                {
                    new S3Object { Key = "key1" },
                    new S3Object { Key = "key2" }
                },
                IsTruncated = false
            };

            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);
            _mockS3Client.Setup(x =>
                    x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectsResponse());

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            cache.Clear();

            _mockS3Client.Verify(
                x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// Tests the Clear method of the S3Cache class to ensure that all objects in the associated S3 bucket
        /// are deleted in batches using pagination. The method simulates a scenario where the bucket contains
        /// more than one page of objects and verifies that the deletion is performed for each batch.
        /// This test sets up mock responses for the Amazon S3 ListObjectsV2Async and DeleteObjectsAsync methods
        /// to emulate paginated retrieval and subsequent deletion of objects in the bucket.
        /// Verifies that:
        /// 1. ListObjectsV2Async is called for each page of objects.
        /// 2. DeleteObjectsAsync is invoked for each batch of objects retrieved.
        /// 3. The Clear method handles both truncated and non-truncated responses appropriately.
        [TestMethod]
        public void Clear_WithPagination_DeletesAllObjectsInBatches()
        {
            var firstResponse = new ListObjectsV2Response
            {
                S3Objects = Enumerable.Range(1, 1000).Select(i => new S3Object { Key = $"key{i}" }).ToList(),
                IsTruncated = true,
                NextContinuationToken = "token1"
            };

            var secondResponse = new ListObjectsV2Response
            {
                S3Objects = Enumerable.Range(1001, 500).Select(i => new S3Object { Key = $"key{i}" }).ToList(),
                IsTruncated = false
            };

            _mockS3Client.SetupSequence(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstResponse)
                .ReturnsAsync(secondResponse);

            _mockS3Client.Setup(x =>
                    x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectsResponse());

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            cache.Clear();

            _mockS3Client.Verify(
                x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        /// <summary>
        /// Clears the current exception if one is set and ensures that no exception is thrown after the operation.
        /// </summary>
        /// <remarks>
        /// This method is intended to reset or clear any existing exceptions within a specific context or scope.
        /// It guarantees that no exceptions will be thrown during or after its execution.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the method is unable to clear the exception successfully due to an invalid state or operation.
        /// </exception>
        [TestMethod]
        public void Clear_Exception_DoesNotThrow()
        {
            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("Error"));

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            cache.Clear();
            // Should not throw
        }

        /// Validates the behavior of the `IsAvailable` method when the S3 bucket exists.
        /// Test Objective:
        /// This unit test ensures that the `IsAvailable` method of the `S3Cache` class
        /// correctly identifies the existence of the specified S3 bucket and returns `true`.
        /// Test Setup and Execution:
        /// - Mocks the S3 client to simulate a successful `GetBucketLocationAsync` call with a valid response.
        /// - Creates an instance of `S3Cache` with the mocked S3 client.
        /// - Calls the `IsAvailable` method to verify its return value when the bucket exists.
        /// Expected Outcome:
        /// If the bucket exists, the `IsAvailable` method should return `true`.
        /// Notes:
        /// - The method under test relies on the `GetBucketLocationAsync` method of the AWS S3 client.
        /// - The behavior of the method is verified based on the proper handling of the mocked response.
        [TestMethod]
        public void IsAvailable_BucketExists_ReturnsTrue()
        {
            _mockS3Client.Setup(x => x.GetBucketLocationAsync(It.Is<GetBucketLocationRequest>(req =>
                    req.BucketName == TestBucketName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetBucketLocationResponse());

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.IsAvailable();

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies the availability status when a specified bucket does not exist.
        /// </summary>
        /// <returns>
        /// Returns <c>false</c> to indicate the bucket is unavailable when it does not exist.
        /// </returns>
        [TestMethod]
        public void IsAvailable_BucketDoesNotExist_ReturnsFalse()
        {
            _mockS3Client.Setup(x => x.GetBucketLocationAsync(It.Is<GetBucketLocationRequest>(req =>
                    req.BucketName == TestBucketName), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("NoSuchBucket"));

            var cache = new S3Cache<string>(TestBucketName, _mockS3Client.Object);
            bool result = cache.IsAvailable();

            Assert.IsFalse(result);
        }

        /// Tests the behavior of the method Keys when using a reverse key converter.
        /// This test verifies that when a valid ListObjectsV2Response containing S3Object keys is returned by the S3 client,
        /// the Keys method correctly maps the S3 object keys back to TKey instances using the provided reverse key converter.
        /// Assertions include:
        /// - The number of keys retrieved from the cache matches the number of S3 objects in the response.
        /// - The values of the retrieved TKey instances match the corresponding keys in the S3 objects.
        [TestMethod]
        public void Keys_WithReverseConverter_ReturnsKeys()
        {
            var listResponse = new ListObjectsV2Response
            {
                S3Objects = new List<S3Object>
                {
                    new S3Object { Key = "key1" },
                    new S3Object { Key = "key2" }
                },
                IsTruncated = false
            };

            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);

            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object,
                k => k.Value, s => new TestKey { Value = s });
            var keys = cache.Keys().ToList();

            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual("key1", keys[0].Value);
            Assert.AreEqual("key2", keys[1].Value);
        }

        /// Tests the behavior of the `Keys` method when it is invoked without a reverse key converter.
        /// This test verifies that the method properly throws a `NotSupportedException` when
        /// an attempt is made to enumerate keys without providing a reverse conversion function.
        /// The test initializes an `S3Cache` instance without a reverse key converter and asserts
        /// that calling the `Keys` method results in a `NotSupportedException`.
        /// Exceptions:
        /// NotSupportedException:
        /// Thrown when the `Keys` method is invoked on an `S3Cache` instance that does not have
        /// a reverse key converter configured.
        [TestMethod]
        public void Keys_WithoutReverseConverter_ThrowsNotSupportedException()
        {
            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object, k => k.Value);
            Assert.ThrowsException<NotSupportedException>(() => cache.Keys().ToList());
        }

        /// Validates that the `Keys` method continues processing and excludes null responses
        /// when iterating over S3 objects fetched from the S3 bucket.
        /// This test ensures that when a `ListObjectsV2Response` from S3 contains null entries
        /// in the `S3Objects` collection, these null entries are skipped, and the valid keys
        /// are correctly returned as part of the enumeration result.
        /// The method sets up a mocked S3 client to return a `ListObjectsV2Response` containing
        /// a mix of valid S3 objects and null entries. It verifies that the `S3Cache.Keys` method
        /// processes only valid S3 objects, excludes nulls, and returns the expected number of valid keys.
        [TestMethod]
        public void Keys_WithNullResponse_ContinuesProcessing()
        {
            var listResponse = new ListObjectsV2Response
            {
                S3Objects = new List<S3Object>
                {
                    new S3Object { Key = "key1" },
                    null,
                    new S3Object { Key = "key2" }
                },
                IsTruncated = false
            };

            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);

            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object,
                k => k.Value, s => new TestKey { Value = s });
            var keys = cache.Keys().ToList();

            Assert.AreEqual(2, keys.Count);
        }

        /// Validates that when retrieving keys from the S3 cache, any entries with null keys are skipped.
        /// This method ensures the `Keys` enumeration excludes null keys by setting up an S3 response
        /// with a mixture of valid and null keys, validating that only non-null keys are returned.
        /// Key behaviors:
        /// - Simulates an S3 object listing through a mock client.
        /// - Constructs an S3Cache instance using the test bucket name and mocked S3 client.
        /// - Confirms that the `Keys` method skips null keys during enumeration.
        /// Assertions:
        /// - The number of non-null keys returned matches the expected count after null keys are skipped.
        [TestMethod]
        public void Keys_WithNullKey_SkipsNullKeys()
        {
            var listResponse = new ListObjectsV2Response
            {
                S3Objects = new List<S3Object>
                {
                    new S3Object { Key = "key1" },
                    new S3Object { Key = null },
                    new S3Object { Key = "key2" }
                },
                IsTruncated = false
            };

            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);

            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object,
                k => k.Value, s => new TestKey { Value = s });
            var keys = cache.Keys().ToList();

            Assert.AreEqual(2, keys.Count);
        }

        /// <summary>
        /// Tests that calling the <see cref="S3Cache{TKey, TValue}.Keys"/> method on an S3Cache instance throws
        /// an <see cref="InvalidOperationException"/> when an <see cref="AmazonS3Exception"/> occurs during the execution.
        /// </summary>
        /// <remarks>
        /// This test ensures that proper exception handling is in place when a failure in listing objects from the S3 bucket
        /// occurs, for instance, when the S3 client operation fails due to an <see cref="AmazonS3Exception"/>.
        /// The test validates that the <see cref="InvalidOperationException"/> is thrown as a result.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the <see cref="S3Cache{TKey, TValue}.Keys"/> method encounters an <see cref="AmazonS3Exception"/>.
        /// </exception>
        [TestMethod]
        public void Keys_S3Exception_ThrowsInvalidOperationException()
        {
            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("Error"));

            var cache = S3Cache<TestKey, string>.Create(TestBucketName, _mockS3Client.Object,
                k => k.Value, s => new TestKey { Value = s });

            Assert.ThrowsException<InvalidOperationException>(() => cache.Keys().ToList());
        }

        /// <summary>
        /// Tests the creation of an S3Cache instance for string keys using the Create method of S3Cache.
        /// Verifies that an instance of S3Cache with string keys is successfully created when valid parameters
        /// are provided, such as a non-empty bucket name and a properly configured S3 client.
        /// </summary>
        [TestMethod]
        public void StringKeyCache_Create_CreatesInstance()
        {
            var cache = S3Cache<string>.Create(TestBucketName, _mockS3Client.Object);
            Assert.IsNotNull(cache);
        }

        /// <summary>
        /// Creates an instance of the StringKeyCache with a specified logger.
        /// </summary>
        /// <param name="logger">
        /// The logger instance used for logging operations within the cache.
        /// </param>
        /// <returns>
        /// A new instance of StringKeyCache configured with the provided logger.
        /// </returns>
        /// <remarks>
        /// This method initializes and returns a StringKeyCache object,
        /// allowing the integration of a logging mechanism to track cache events and operations.
        /// </remarks>
        [TestMethod]
        public void StringKeyCache_CreateWithLogger_CreatesInstance()
        {
            var cache = S3Cache<string>.Create(TestBucketName, _mockS3Client.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger>());
            Assert.IsNotNull(cache);
        }

        /// <summary>
        /// Tests that the <see cref="S3Cache{TValue}.Keys"/> method correctly retrieves all string keys
        /// from an S3 bucket when using an instance of <see cref="S3Cache{TValue}"/>.
        /// </summary>
        /// <remarks>
        /// This test verifies that the method:
        /// 1. Retrieves all objects from the S3 bucket.
        /// 2. Extracts the keys as strings from the objects.
        /// 3. Handles non-truncated results properly.
        /// The test validates the functionality by asserting the expected number of keys and their
        /// correctness based on a mocked S3 response.
        /// </remarks>
        /// <exception cref="AssertFailedException">
        /// Thrown when the number or the value of the keys retrieved does not match the expected result.
        /// </exception>
        [TestMethod]
        public void StringKeyCache_Keys_ReturnsStringKeys()
        {
            var listResponse = new ListObjectsV2Response
            {
                S3Objects = new List<S3Object>
                {
                    new S3Object { Key = "key1" },
                    new S3Object { Key = "key2" }
                },
                IsTruncated = false
            };

            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);

            var cache = S3Cache<string>.Create(TestBucketName, _mockS3Client.Object);
            var keys = cache.Keys().ToList();

            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual("key1", keys[0]);
            Assert.AreEqual("key2", keys[1]);
        }

        /// Tests that the Keys method of the S3Cache throws an InvalidOperationException
        /// when an AmazonS3Exception is encountered during the retrieval of keys.
        /// This scenario simulates an exception raised from the ListObjectsV2Async
        /// method of the Amazon S3 client, which is typically called when retrieving
        /// keys from the S3 bucket.
        /// The test ensures that the cache's implementation properly handles errors
        /// from the underlying cloud storage service and correctly raises a higher-level
        /// exception to indicate the failure of the operation.
        [TestMethod]
        public void StringKeyCache_Keys_S3Exception_ThrowsInvalidOperationException()
        {
            _mockS3Client.Setup(x =>
                    x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("Error"));

            var cache = S3Cache<string>.Create(TestBucketName, _mockS3Client.Object);

            Assert.ThrowsException<InvalidOperationException>(() => cache.Keys().ToList());
        }
    }

    /// <summary>
    /// Represents a key used in caching operations.
    /// </summary>
    public class TestKey
    {
        /// <summary>
        /// Gets or sets the value associated with the object.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string representation of the current object.</returns>
        public override string ToString()
        {
            return Value;
        }
    }
}