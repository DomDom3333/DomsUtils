using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomsUtils.Services.Caching.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DomsUtils.Tests.Services.Caching.Bases;

/// <summary>
/// A test class for validating the behavior of the MemoryCache class.
/// </summary>
/// <remarks>
/// This class contains unit tests for different operations supported by the MemoryCache class,
/// such as adding, retrieving, removing, and clearing items in the cache. It also includes tests
/// for event handling, edge cases, thread safety, and performance under different scenarios.
/// </remarks>
/// <example>
/// Common operations tested include constructors, TryGet, Set, Remove, Clear, and retrieving keys.
/// Specific tests address scenarios such as handling of null or empty keys, cache availability,
/// and behavior with complex or large datasets.
/// </example>
[TestClass]
public class MemoryCacheTests
{
    /// <summary>
    /// Mock instance of <see cref="ILogger"/> used for testing and verifying log-related functionality
    /// in the <see cref="MemoryCacheTests"/> class.
    /// This mock facilitates validation of log messages and ensures expected interactions
    /// with the logger during test executions.
    /// </summary>
    private Mock<ILogger> _mockLogger;

    /// <summary>
    /// Represents a private instance of the <see cref="MemoryCache{TKey, TValue}"/> class,
    /// used for testing caching mechanisms and functionality.
    /// </summary>
    /// <remarks>
    /// This variable is initialized and used within the test methods of the <c>MemoryCacheTests</c> class.
    /// It allows for the validation of operations such as adding, retrieving, updating, and removing cache entries.
    /// </remarks>
    private MemoryCache<string, string> _cache;

    /// <summary>
    /// Represents an instance of a <see cref="MemoryCache{TKey, TValue}"/> configured with a logger.
    /// Used for testing purposes in the context of caching operations that require logging functionality.
    /// </summary>
    private MemoryCache<string, string> _cacheWithLogger;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger>();
        _cache = new MemoryCache<string, string>();
        _cacheWithLogger = new MemoryCache<string, string>(_mockLogger.Object);
    }

    /// <summary>
    /// Cleans up resources or state after each test method execution.
    /// This method is executed automatically after each test in the
    /// MemoryCacheTests test class, ensuring that cache instances are cleared
    /// and do not retain state between tests.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        _cache?.Clear();
        _cacheWithLogger?.Clear();
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that the default constructor of the <see cref="MemoryCache{TKey, TValue}"/> class
    /// initializes the cache instance successfully.
    /// </summary>
    /// <remarks>
    /// This test checks the proper instantiation of the MemoryCache object using the default constructor
    /// and ensures that the cache is available for use after initialization.
    /// </remarks>
    [TestMethod]
    public void Constructor_Default_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var cache = new MemoryCache<string, int>();

        // Assert
        Assert.IsNotNull(cache);
        Assert.IsTrue(cache.IsAvailable());
    }

    /// <summary>
    /// Validates that the constructor of the MemoryCache class initializes successfully when provided with a logger instance.
    /// </summary>
    /// <remarks>
    /// This test ensures the MemoryCache instance is created without throwing errors and verifies its availability after initialization.
    /// </remarks>
    /// <seealso cref="MemoryCache{TKey, TValue}"/>
    [TestMethod]
    public void Constructor_WithLogger_ShouldInitializeSuccessfully()
    {
        // Arrange
        var logger = new Mock<ILogger>().Object;

        // Act
        var cache = new MemoryCache<string, int>(logger);

        // Assert
        Assert.IsNotNull(cache);
        Assert.IsTrue(cache.IsAvailable());
    }

    /// <summary>
    /// Tests the constructor of the <see cref="MemoryCache{TKey, TValue}"/> class when a null logger is provided.
    /// </summary>
    /// <remarks>
    /// Ensures that the cache is initialized successfully even if the logger argument is null.
    /// Verifies that the created cache instance is not null and that the cache is marked as
    [TestMethod]
    public void Constructor_WithNullLogger_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var cache = new MemoryCache<string, int>(null!);

        // Assert
        Assert.IsNotNull(cache);
        Assert.IsTrue(cache.IsAvailable());
    }

    #endregion

    #region TryGet Tests

    /// Tests whether the `TryGet` method of the `MemoryCache` retrieves a stored value using an existing key.
    /// This test case ensures that calling `TryGet` with a valid key:
    /// 1. Returns `true` as the result.
    /// 2. Retrieves the correct value associated with the given key.
    /// Preconditions:
    /// - The cache must be initialized before running this test.
    /// - A key-value pair must be added to the cache prior to invoking `TryGet` with the key.
    /// Test Steps:
    /// 1. Set a key-value pair in the cache.
    /// 2. Call the `TryGet` method with the given key.
    /// 3. Verify that the result is `true`.
    /// 4. Verify that the value retrieved matches the expected value.
    /// Expected Results:
    /// - The `TryGet` method should return `true`.
    /// - The `actualValue` retrieved should match the value associated with the given key.
    [TestMethod]
    public void TryGet_ExistingKey_ShouldReturnTrueAndValue()
    {
        // Arrange
        const string key = "testKey";
        const string expectedValue = "testValue";
        _cache.Set(key, expectedValue);

        // Act
        bool result = _cache.TryGet(key, out string? actualValue);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(expectedValue, actualValue);
    }

    /// Tests the behavior of the TryGet method when attempting to retrieve a value using a non-existing key.
    /// Ensures the method returns false and outputs the default value of the type.
    /// Test Steps:
    /// 1. Arrange: Set up a key that is not present in the cache.
    /// 2. Act: Call the TryGet method using the non-existing key.
    /// 3. Assert:
    /// - Verify the method returns false.
    /// - Verify the output value is equal to the default value of the type.
    [TestMethod]
    public void TryGet_NonExistingKey_ShouldReturnFalseAndDefaultValue()
    {
        // Arrange
        const string key = "nonExistingKey";

        // Act
        bool result = _cache.TryGet(key, out string? actualValue);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(actualValue);
    }

    [TestMethod]
    public void TryGet_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _cache.TryGet(null!, out _));
    }

    /// <summary>
    /// Tests the behavior of the <see cref="MemoryCache{TKey, TValue}.TryGet"/> method when an empty string is used as the key.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="MemoryCache{TKey, TValue}"/> correctly retrieves a value associated with an empty key.
    /// The test verifies both the return value of the method and the actual value retrieved from the cache.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown when the assertion that the method returns true or
    [TestMethod]
    public void TryGet_EmptyKey_ShouldWorkCorrectly()
    {
        // Arrange
        const string key = "";
        const string value = "emptyKeyValue";
        _cache.Set(key, value);

        // Act
        bool result = _cache.TryGet(key, out string? actualValue);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(value, actualValue);
    }

    #endregion

    #region Set Tests

    /// Validates that a newly added key-value pair is successfully stored in the cache.
    /// This test method is designed to ensure that when a new key-value pair is added
    /// to the memory cache, it can be retrieved afterwards using the specified key.
    /// It asserts that the retrieval operation succeeds and that the retrieved value
    /// matches the value that was originally set.
    [TestMethod]
    public void Set_NewKeyValue_ShouldAddToCache()
    {
        // Arrange
        const string key = "newKey";
        const string value = "newValue";

        // Act
        _cache.Set(key, value);

        // Assert
        bool exists = _cache.TryGet(key, out string? retrievedValue);
        Assert.IsTrue(exists);
        Assert.AreEqual(value, retrievedValue);
    }

    /// <summary>
    /// Tests the behavior of the <see cref="MemoryCache{TKey, TValue}.Set"/> method when invoked with an existing key.
    /// Ensures that the value associated with the existing key is updated correctly.
    /// </summary>
    /// <remarks>
    /// This test verifies that calling the Set method with the same key overwrites the previous value.
    /// Additionally, it confirms that the value is retrievable via the <see
    [TestMethod]
    public void Set_ExistingKey_ShouldUpdateValue()
    {
        // Arrange
        const string key = "existingKey";
        const string originalValue = "originalValue";
        const string updatedValue = "updatedValue";
        _cache.Set(key, originalValue);

        // Act
        _cache.Set(key, updatedValue);

        // Assert
        bool exists = _cache.TryGet(key, out string? retrievedValue);
        Assert.IsTrue(exists);
        Assert.AreEqual(updatedValue, retrievedValue);
    }

    /// <summary>
    /// Validates that invoking the <see cref="MemoryCache{TKey, TValue}.Set"/> method with a null key throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="MemoryCache{TKey, TValue}"/> correctly enforces non-null constraints for keys
    /// when storing values in the cache. Null keys should not be allowed, and an appropriate exception is expected to be thrown.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="key"/> parameter in the <see cref="MemoryCache{TKey, TValue}.Set"/> method is null.
    /// </exception>
    [TestMethod]
    public void Set_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _cache.Set(null!, "value"));
    }

    [TestMethod]
    public void Set_NullValue_ShouldStoreNullValue()
    {
        // Arrange
        const string key = "nullValueKey";

        // Act
        _cache.Set(key, null!);

        // Assert
        bool exists = _cache.TryGet(key, out string? retrievedValue);
        Assert.IsTrue(exists);
        Assert.IsNull(retrievedValue);
    }

    /// Validates that the OnSet event is triggered when a key-value pair is added to the cache.
    /// It ensures that the event provides the correct key and value, and confirms that
    /// the event is being invoked as expected during the operation.
    [TestMethod]
    public void Set_ShouldTriggerOnSetEvent()
    {
        // Arrange
        const string key = "eventKey";
        const string value = "eventValue";
        string? eventKey = null;
        string? eventValue = null;
        bool eventTriggered = false;

        _cache.OnSet += (k, v) =>
        {
            eventKey = k;
            eventValue = v;
            eventTriggered = true;
        };

        // Act
        _cache.Set(key, value);

        // Assert
        Assert.IsTrue(eventTriggered);
        Assert.AreEqual(key, eventKey);
        Assert.AreEqual(value, eventValue);
    }

    /// Tests that when multiple event subscribers are set for the OnSet event,
    /// all subscribers are triggered upon calling the Set method.
    /// This test registers multiple event subscribers to the OnSet event of the memory cache.
    /// The subscribers increment a shared counter when triggered. After setting a key-value pair
    /// to the cache, the test verifies that the number of triggered events matches the number
    /// of subscribers.
    /// Preconditions:
    /// - A MemoryCache instance is initialized and subscribes multiple handlers to the OnSet event.
    /// Postconditions:
    /// - The OnSet event is fired for each subscriber when the Set method is invoked.
    /// Verifications:
    /// - The number of times the On
    [TestMethod]
    public void Set_MultipleEventSubscribers_ShouldTriggerAllSubscribers()
    {
        // Arrange
        const string key = "multiEventKey";
        const string value = "multiEventValue";
        int eventCount = 0;

        _cache.OnSet += (k, v) => eventCount++;
        _cache.OnSet += (k, v) => eventCount++;

        // Act
        _cache.Set(key, value);

        // Assert
        Assert.AreEqual(2, eventCount);
    }

    #endregion

    #region Remove Tests

    /// Validates that calling the `Remove` method with an existing key successfully removes the associated
    /// key-value pair from the cache and returns true.
    /// The method first adds a key-value pair to the cache, then attempts to remove the key using
    /// the `Remove` method. Asserts are performed to check the following:
    /// 1. The `Remove` method returns true, indicating the operation was successful.
    /// 2. The key does not exist in the cache after removal.
    [TestMethod]
    public void Remove_ExistingKey_ShouldReturnTrueAndRemoveItem()
    {
        // Arrange
        const string key = "removeKey";
        const string value = "removeValue";
        _cache.Set(key, value);

        // Act
        bool result = _cache.Remove(key);

        // Assert
        Assert.IsTrue(result);
        bool exists = _cache.TryGet(key, out _);
        Assert.IsFalse(exists);
    }

    /// Tests the behavior of the cache when attempting to remove a non-existing key.
    /// Ensures that the method returns false when the key does not exist in the cache.
    [TestMethod]
    public void Remove_NonExistingKey_ShouldReturnFalse()
    {
        // Arrange
        const string key = "nonExistingRemoveKey";

        // Act
        bool result = _cache.Remove(key);

        // Assert
        Assert.IsFalse(result);
    }

    /// Tests the Remove method of the MemoryCache to ensure it throws an ArgumentNullException
    /// when a null key is provided.
    /// This test validates that the Remove method in the MemoryCache implementation does not
    /// accept null keys and enforces input validation by throwing an ArgumentNullException.
    /// This behavior ensures that the cache only operates on valid key values.
    /// Throws:
    /// ArgumentNullException: If the provided key is null.
    [TestMethod]
    public void Remove_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _cache.Remove(null!));
    }

    /// Validates that attempting to remove an item from an empty cache returns false.
    /// This test ensures the `MemoryCache` implementation correctly handles removal
    /// requests when no items exist in the cache.
    /// Test Steps:
    /// 1. Instantiate an empty `MemoryCache` object.
    /// 2. Attempt to remove a key from the empty cache.
    /// 3. Verify that the method returns false.
    /// Expected Result:
    /// - The `Remove` method should return false, indicating the key was not present
    /// and no removal occurred.
    [TestMethod]
    public void Remove_FromEmptyCache_ShouldReturnFalse()
    {
        // Arrange
        var emptyCache = new MemoryCache<string, string>();
        const string key = "anyKey";

        // Act
        bool result = emptyCache.Remove(key);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region Clear Tests

    /// <summary>
    /// Tests the behavior of the Clear method when invoked on an empty cache.
    /// Ensures that no exceptions are thrown during the operation.
    /// </summary>
    [TestMethod]
    public void Clear_EmptyCache_ShouldNotThrow()
    {
        // Arrange
        var emptyCache = new MemoryCache<string, string>();

        // Act & Assert
        emptyCache.Clear(); // Should not throw
    }

    [TestMethod]
    public void Clear_CacheWithItems_ShouldRemoveAllItems()
    {
        // Arrange
        _cache.Set("key1", "value1");
        _cache.Set("key2", "value2");
        _cache.Set("key3", "value3");

        // Act
        _cache.Clear();

        // Assert
        Assert.IsFalse(_cache.TryGet("key1", out _));
        Assert.IsFalse(_cache.TryGet("key2", out _));
        Assert.IsFalse(_cache.TryGet("key3", out _));
        CollectionAssert.AreEqual(new string[0], _cache.Keys().ToArray());
    }

    /// Tests the behavior of the `Clear` method when invoked multiple times consecutively.
    /// Ensures that multiple calls to the `Clear` method do not throw any exceptions,
    /// even if the cache is already empty.
    /// This test verifies robustness of the caching mechanism when clearing the cache
    /// multiple times, simulating real-world scenarios where `Clear` might be called
    /// redundantly or under varying conditions.
    [TestMethod]
    public void Clear_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        _cache.Set("key", "value");

        // Act & Assert
        _cache.Clear();
        _cache.Clear(); // Should not throw
        _cache.Clear(); // Should not throw
    }

    #endregion

    #region Keys Tests

    /// Tests the Keys method of the MemoryCache class when the cache is empty.
    /// Verifies that the method returns an empty collection in this scenario.
    /// This is a unit test to ensure that calling Keys on an empty MemoryCache instance
    /// returns a non-null, empty enumerable collection.
    /// Test Steps:
    /// 1. Arrange: Create an instance of MemoryCache with no items.
    /// 2. Act: Retrieve the keys using the Keys method.
    /// 3. Assert: Verify that the returned collection is not null and is empty.
    /// This test ensures that the behavior of the Keys method correctly reflects the state of an empty cache.
    [TestMethod]
    public void Keys_EmptyCache_ShouldReturnEmptyCollection()
    {
        // Arrange
        var emptyCache = new MemoryCache<string, string>();

        // Act
        var keys = emptyCache.Keys();

        // Assert
        Assert.IsNotNull(keys);
        CollectionAssert.AreEqual(new string[0], keys.ToArray());
    }

    /// <summary>
    /// Tests that the Keys method of the cache returns all keys when the cache contains items.
    /// </summary>
    /// <remarks>
    /// This test verifies that the Keys method correctly reflects all the keys present in the cache after adding items.
    /// It ensures the returned collection matches the expected keys by performing an equality check on the count
    /// and validating that all expected keys are contained in the result.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown if the count of keys does not match or any expected key is missing in the returned keys.
    /// </exception>
    [TestMethod]
    public void Keys_CacheWithItems_ShouldReturnAllKeys()
    {
        // Arrange
        var expectedKeys = new[] { "key1", "key2", "key3" };
        foreach (var key in expectedKeys)
        {
            _cache.Set(key, $"value_{key}");
        }

        // Act
        var actualKeys = _cache.Keys().ToList();

        // Assert
        Assert.AreEqual(expectedKeys.Length, actualKeys.Count);
        foreach (var key in expectedKeys)
        {
            CollectionAssert.Contains(actualKeys, key);
        }
    }

    /// <summary>
    /// Verifies that the collection of keys in the cache reflects the current state of the cache after various modifications.
    /// </summary>
    /// <remarks>
    /// This unit test performs the following operations:
    /// - Validates the initial collection of keys after adding items to the cache.
    /// - Checks the state of keys after adding a new item.
    /// - Ensures the key collection is accurate after removing a specific key.
    /// - Confirms that clearing the cache results in an empty key collection.
    /// Uses assertions to compare the expected and actual state of the key collection throughout the modifications.
    /// </remarks>
    [TestMethod]
    public void Keys_AfterModifications_ShouldReflectCurrentState()
    {
        // Arrange
        _cache.Set("key1", "value1");
        _cache.Set("key2", "value2");

        // Act - Initial state
        var initialKeys = _cache.Keys().ToList();

        // Add key
        _cache.Set("key3", "value3");
        var afterAddKeys = _cache.Keys().ToList();

        // Remove key
        _cache.Remove("key1");
        var afterRemoveKeys = _cache.Keys().ToList();

        // Clear
        _cache.Clear();
        var afterClearKeys = _cache.Keys().ToList();

        // Assert
        Assert.AreEqual(2, initialKeys.Count);
        Assert.AreEqual(3, afterAddKeys.Count);
        Assert.AreEqual(2, afterRemoveKeys.Count);
        Assert.AreEqual(0, afterClearKeys.Count);
    }

    #endregion

    #region IsAvailable Tests

    /// Verifies that a newly created `MemoryCache` instance is available for use.
    /// This test checks the functionality of the `IsAvailable` method to ensure
    /// it returns true for a freshly initialized cache.
    /// The test initializes a new instance of `MemoryCache`, which should have
    /// no pre-existing data, and invokes the `IsAvailable` method. The expectation
    /// is that the method returns true, confirming the cache instance's readiness
    [TestMethod]
    public void IsAvailable_NewCache_ShouldReturnTrue()
    {
        // Arrange
        var newCache = new MemoryCache<string, string>();

        // Act
        bool isAvailable = newCache.IsAvailable();

        // Assert
        Assert.IsTrue(isAvailable);
    }

    [TestMethod]
    public void IsAvailable_CacheWithData_ShouldReturnTrue()
    {
        // Arrange
        _cache.Set("testKey", "testValue");

        // Act
        bool isAvailable = _cache.IsAvailable();

        // Assert
        Assert.IsTrue(isAvailable);
    }

    /// <summary>
    /// Tests the behavior of the <see cref="MemoryCache{TKey, TValue}.IsAvailable"/> method when integer keys are used in the cache.
    /// Validates that the cache correctly handles the key type and determines availability.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="MemoryCache{TKey, TValue}"/> with integer keys is functional and that the
    /// availability check is appropriately implemented for this type of key. The test creates an integer-keyed cache,
    /// verifies the availability status, and ensures expected operation without any type-related issues.
    /// </remarks>
    [TestMethod]
    public void IsAvailable_IntegerKeys_ShouldHandleKeyTypeCorrectly()
    {
        // Arrange
        var intCache = new MemoryCache<int, string>();

        // Act
        bool isAvailable = intCache.IsAvailable();

        // Assert
        Assert.IsTrue(isAvailable);
    }

    #endregion

    #region Thread Safety Tests

    /// <summary>
    /// Tests the thread-safety of the memory cache by performing concurrent operations
    /// such as Add, Retrieve, and Remove using multiple threads.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="MemoryCache{TKey, TValue}"/> implementation
    /// can handle concurrent read and write operations without introducing data corruption,
    /// race conditions, or unexpected behavior.
    /// It involves creating multiple threads, each performing a series of cache operations such as
    /// setting key-value pairs, retrieving values, and removing them from the cache.
    /// At the end of the operation, the cache should remain in a consistent and operational state,
    /// confirming its thread safety.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    [TestMethod]
    public void ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var cache = new MemoryCache<int, string>();
        const int numberOfThreads = 10;
        const int operationsPerThread = 100;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < numberOfThreads; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    int key = threadId * operationsPerThread + j;
                    cache.Set(key, $"value_{key}");
                    cache.TryGet(key, out _);
                    if (j % 2 == 0)
                    {
                        cache.Remove(key);
                    }
                }
            }));
        }

        // Assert
        Task.WaitAll(tasks.ToArray());
        Assert.IsTrue(cache.IsAvailable());
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Validates the complete workflow for the <see cref="MemoryCache{TKey, TValue}"/> class, ensuring its functionality
    /// is correct throughout the lifecycle of various operations.
    /// </summary>
    /// <remarks>
    /// This test verifies that the caching mechanism correctly handles the following scenarios:
    /// - Adding multiple key-value pairs to the cache.
    /// - Retrieving values by their keys and asserting their correctness.
    /// - Updating existing values.
    /// - Removing individual items from the cache.
    /// - Verifying the keys present in the cache after operations.
    /// - Clearing all items from the cache.
    /// - Ensuring the cache remains operational after clearing.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown when any assertion related to cache behavior (e.g., value retrieval, key presence, count verification) fails.
    /// </exception>
    [TestMethod]
    public void CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var cache = new MemoryCache<string, int>();

        // Act & Assert - Set multiple values
        cache.Set("one", 1);
        cache.Set("two", 2);
        cache.Set("three", 3);

        // Verify all values exist
        Assert.IsTrue(cache.TryGet("one", out int value1));
        Assert.AreEqual(1, value1);
        Assert.IsTrue(cache.TryGet("two", out int value2));
        Assert.AreEqual(2, value2);
        Assert.IsTrue(cache.TryGet("three", out int value3));
        Assert.AreEqual(3, value3);

        // Verify keys collection
        var keys = cache.Keys().ToList();
        Assert.AreEqual(3, keys.Count);

        // Update existing value
        cache.Set("two", 22);
        Assert.IsTrue(cache.TryGet("two", out int updatedValue));
        Assert.AreEqual(22, updatedValue);

        // Remove one item
        Assert.IsTrue(cache.Remove("one"));
        Assert.IsFalse(cache.TryGet("one", out _));

        // Verify remaining items
        Assert.AreEqual(2, cache.Keys().Count());

        // Clear all
        cache.Clear();
        Assert.AreEqual(0, cache.Keys().Count());
        Assert.IsFalse(cache.TryGet("two", out _));
        Assert.IsFalse(cache.TryGet("three", out _));

        // Verify cache is still available
        Assert.IsTrue(cache.IsAvailable());
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Validates that the MemoryCache can correctly handle complex types as values, such as lists or other collections.
    /// </summary>
    /// <remarks>
    /// This method sets and retrieves a list of integers as a value in the memory cache, ensuring:
    /// - The set operation correctly adds the item to the cache.
    /// - The retrieve operation (`TryGet`) successfully retrieves the item.
    /// - The retrieved value matches the originally stored value in terms of count and sequence.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown if any assertion related to the cache operations fails.
    /// </exception>
    [TestMethod]
    public void MemoryCache_WithComplexTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var complexCache = new MemoryCache<string, List<int>>();
        var testList = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        complexCache.Set("list", testList);

        // Assert
        Assert.IsTrue(complexCache.TryGet("list", out List<int>? retrievedList));
        Assert.IsNotNull(retrievedList);
        Assert.AreEqual(testList.Count, retrievedList.Count);
        CollectionAssert.AreEqual(testList, retrievedList);
    }

    /// Verifies that the MemoryCache can handle value types as values correctly.
    /// This test ensures that a value type (e.g., an integer) can be stored, retrieved, and validated without issues in the cache.
    /// Specifically, it confirms that:
    /// 1. The cache successfully stores a value type.
    /// 2. The `TryGet` method returns `true` when retrieving a stored key.
    /// 3. The retrieved value matches the originally stored value.
    [TestMethod]
    public void MemoryCache_WithValueTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var valueTypeCache = new MemoryCache<string, int>();

        // Act
        valueTypeCache.Set("number", 42);

        // Assert
        Assert.IsTrue(valueTypeCache.TryGet("number", out int retrievedNumber));
        Assert.AreEqual(42, retrievedNumber);
    }

    /// <summary>
    /// Verifies that the <c>OnSet</c> event in the <c>MemoryCache</c> class does not throw an exception
    /// when the event has a null subscriber and a set operation is performed.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <c>OnSet</c> event is safely handled even when no subscribers
    /// are attached, maintaining the robustness of the <c>Set</c> method in the cache implementation.
    /// </remarks>
    [TestMethod]
    public void OnSetEvent_WithNullSubscriber_ShouldNotThrow()
    {
        // Arrange
        var cache = new MemoryCache<string, string>();
        cache.OnSet += null!; // This should be handled gracefully

        // Act & Assert
        cache.Set("key", "value"); // Should not throw
    }

    #endregion

    #region Performance Tests

    /// <summary>
    /// Validates the performance and correctness of the cache's functionality under a large data set.
    /// Ensures that the operations such as adding items, retrieving items, and verifying keys work
    /// as expected when the cache contains a significant number of elements.
    /// </summary>
    /// <remarks>
    /// This test method adds a large number of items to the cache and verifies its ability to handle
    /// the operations within an acceptable time limit. The test ensures that:
    /// - All items are successfully stored and can be retrieved.
    /// - A specific item in the middle of the set is correctly accessed and matches the expected value.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown when any
    [TestMethod]
    [Timeout(5000)] // 5 second timeout
    public void LargeDataSet_ShouldPerformReasonably()
    {
        // Arrange
        var cache = new MemoryCache<int, string>();
        const int itemCount = 10000;

        // Act
        for (int i = 0; i < itemCount; i++)
        {
            cache.Set(i, $"value_{i}");
        }

        // Assert
        Assert.AreEqual(itemCount, cache.Keys().Count());
        Assert.IsTrue(cache.TryGet(5000, out string? value));
        Assert.AreEqual("value_5000", value);
    }

    /// <summary>
    /// Validates that the MemoryCache class can handle various data types for keys and values.
    /// </summary>
    /// <remarks>
    /// This test ensures that different key-value pair data types, such as string-string, int-int, and Guid-DateTime,
    /// can be correctly stored, retrieved, and validated in the cache.
    /// The method performs the following checks:
    /// - Verifies successful storage and retrieval of string key-value pairs.
    /// - Ensures that integer key-value pairs are correctly handled.
    /// - Confirms that GUID-based keys with DateTime values can be stored and retrieved accurately.
    /// Useful for verifying the library's flexibility and robustness in handling different generic data types.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown when any of the assertions within the test fail, indicating some incompatibility or incorrect behavior
    /// with handling the specified data types.
    /// </exception>
    [TestMethod]
    public void DataTypes_ShouldHandleVariousTypes()
    {
        // Test with different data types
        var stringCache = new MemoryCache<string, string>();
        var intCache = new MemoryCache<int, int>();
        var guidCache = new MemoryCache<Guid, DateTime>();

        // String cache
        stringCache.Set("test", "value");
        Assert.IsTrue(stringCache.TryGet("test", out string? stringValue));
        Assert.AreEqual("value", stringValue);

        // Int cache
        intCache.Set(42, 84);
        Assert.IsTrue(intCache.TryGet(42, out int intValue));
        Assert.AreEqual(84, intValue);

        // Guid cache
        var guid = Guid.NewGuid();
        var now = DateTime.Now;
        guidCache.Set(guid, now);
        Assert.IsTrue(guidCache.TryGet(guid, out DateTime dateValue));
        Assert.AreEqual(now, dateValue);
    }

    #endregion
}