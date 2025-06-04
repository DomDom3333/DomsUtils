using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using DomsUtils.Services.Caching.Bases;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DomsUtils.Tests.Services.Caching.Bases;

/// <summary>
/// Unit test class for the <see cref="FileCache{TKey, TValue}"/> implementation.
/// Responsible for ensuring the correct behavior of file-based caching functionality
/// by testing various scenarios such as setting, retrieving, removing items,
/// and handling edge cases like null inputs, concurrency, and persistence.
/// </summary>
/// <remarks>
/// The test verifies core functionalities including:
/// - Initialization with valid or invalid parameters.
/// - Data persistence across instances.
/// - Proper handling of corrupted or missing files.
/// - Ensuring file system interactions align with expected behaviors.
/// - Coverage for specialized methods such as IsAvailable, Clear, and Keys.
/// </remarks>
[TestClass]
[TestSubject(typeof(FileCache<,>))]
public class FileCacheTest
{
    /// <summary>
    /// Stores the file path to a temporary directory used for testing purposes.
    /// This directory is dynamically generated during test setup and cleaned up after tests are completed.
    /// </summary>
    private string _tempDirectory = string.Empty;

    /// <summary>
    /// Represents a private instance of the <see cref="FileCache{TKey, TValue}"/> class,
    /// used for testing file-based caching functionality within the <see cref="FileCacheTest"/> test class.
    /// </summary>
    /// <remarks>
    /// This variable is initialized in the <see cref="Setup"/> method and tested in various test methods
    /// to confirm proper behavior of the <see cref="FileCache{TKey, TValue}"/> class.
    /// It acts as the in-memory representation of the file cache during test execution.
    /// </remarks>
    private FileCache<string, TestData> _cache = null!;

    /// <summary>
    /// An instance of <see cref="ILogger{TCategoryName}"/> used to log information, warnings, and errors
    /// in the context of testing the <see cref="FileCache{TKey, TValue}"/> class.
    /// </summary>
    private ILogger<FileCache<string, TestData>> _logger = null!;

    /// <summary>
    /// Sets up the necessary resources and initialization logic for the FileCacheTest class.
    /// This method is automatically invoked before each test case execution in the FileCacheTest
    /// unit test class.
    /// </summary>
    /// <remarks>
    /// - It creates a temporary directory for use as the backing storage of the file-based cache.
    /// - An instance of a test logger is initialized.
    /// - A new instance of the FileCache is created, using the temporary directory and logger.
    /// </remarks>
    [TestInitialize]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _logger = new TestLogger<FileCache<string, TestData>>();
        _cache = new FileCache<string, TestData>(_tempDirectory, _logger);
    }

    /// <summary>
    /// Cleans up test resources after a test method execution completes.
    /// </summary>
    /// <remarks>
    /// Deletes the temporary directory used for testing, if it exists.
    /// This ensures that no temporary files or directories persist after the test execution.
    /// </remarks>
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    #region Constructor Tests

    /// <summary>
    /// Validates that the <see cref="FileCache{TKey, TValue}"/> constructor creates a new directory
    /// if the specified path does not already exist.
    /// </summary>
    /// <remarks>
    /// This test ensures that when a valid but non-existent directory path is provided,
    /// the directory is automatically created by the cache implementation.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown if the test assertion that the directory exists after instantiation fails.
    /// </exception>
    [TestMethod]
    public void Constructor_WithValidPath_CreatesDirectoryIfNotExists()
    {
        // Arrange
        string newTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var cache = new FileCache<string, TestData>(newTempDir);

        // Assert
        Assert.IsTrue(Directory.Exists(newTempDir));

        // Cleanup
        Directory.Delete(newTempDir, true);
    }

    /// <summary>
    /// Verifies that the constructor of the <see cref="FileCache{TKey, TValue}"/> class
    /// does not throw an exception when the specified directory already exists.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="FileCache{TKey, TValue}"/> instance can
    /// initialize successfully without errors when provided an existing and valid directory.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown if the <see cref="FileCache{TKey, TValue}"/> constructor does not behave as expected
    /// with an existing directory.
    /// </exception>
    [TestMethod]
    public void Constructor_WithExistingDirectory_DoesNotThrow()
    {
        // Arrange
        string existingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(existingDir);

        // Act & Assert
        var cache = new FileCache<string, TestData>(existingDir);
        Assert.IsNotNull(cache);

        // Cleanup
        Directory.Delete(existingDir, true);
    }

    /// <summary>
    /// Tests that the <see cref="FileCache{TKey, TValue}"/> constructor handles a null logger
    /// by using a default logger implementation, specifically <see cref="NullLogger"/>.
    /// </summary>
    /// <remarks>
    /// Verifies that the <see cref="FileCache{TKey, TValue}"/> instance is successfully created
    /// even when the logger parameter is null, and ensures that the resulting cache object is not null.
    /// </remarks>
    /// <example>
    /// This test ensures that passing a null logger to the <see cref="FileCache{TKey, TValue}"/> constructor
    /// does not cause a failure and uses the default logging mechanism.
    /// </example>
    [TestMethod]
    public void Constructor_WithNullLogger_UsesNullLogger()
    {
        // Arrange & Act
        var cache = new FileCache<string, TestData>(_tempDirectory, null);

        // Assert
        Assert.IsNotNull(cache);
    }

    #endregion

    #region TryGet Tests

    /// Tests the TryGet method of the FileCache class with a non-existent key.
    /// Validates that the method returns false and outputs a null value when an
    /// attempt is made to retrieve a key that does not exist in the cache.
    /// This ensures that the implementation correctly handles scenarios where
    /// the requested key is missing.
    /// Preconditions:
    /// - The file cache is initialized and ready for use.
    /// Postconditions:
    /// - The TryGet method returns false.
    /// - The output value parameter is set to null.
    /// Test Scenarios:
    /// - Verify that calling TryGet with a key that does not exist in the cache
    /// produces the expected result.
    [TestMethod]
    public void TryGet_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        bool result = _cache.TryGet("nonexistent", out TestData value);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    /// Verifies that the `TryGet` method of the `FileCache` class returns true when attempting to retrieve an item
    /// using a key that exists in the cache. Additionally, verifies that the retrieved value matches the expected data.
    /// This test ensures proper functionality of the `TryGet` method in scenarios where the specified key is present
    /// in the cache storage and associated data is correctly retrieved and validated.
    [TestMethod]
    public void TryGet_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };
        _cache.Set("key1", testData);

        // Act
        bool result = _cache.TryGet("key1", out TestData value);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(value);
        Assert.AreEqual(testData.Id, value.Id);
        Assert.AreEqual(testData.Name, value.Name);
    }

    /// Validates the behavior of the TryGet method when the corresponding file for a cached item is deleted externally.
    /// Ensures that the method returns false when the file is no longer present and cleans up the internal mapping for the missing entry.
    /// This test covers the following scenarios:
    /// - The cache is initialized, and an item is added and stored in a file.
    /// - The file representing the item is deleted externally outside the application.
    /// - When TryGet is called for the deleted file, it should return false and set the output value to null.
    /// - The internal mapping should also be cleaned up to ensure no orphaned keys remain.
    [TestMethod]
    public void TryGet_WhenFileDeletedExternally_ReturnsFalseAndCleansMapping()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };
        _cache.Set("key1", testData);

        // Delete the file externally
        string[] files = Directory.GetFiles(_tempDirectory, "*.json");
        string dataFile = files.FirstOrDefault(f => !f.EndsWith("_keymapping.json"));
        if (dataFile != null)
        {
            File.Delete(dataFile);
        }

        // Act
        bool result = _cache.TryGet("key1", out TestData value);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    #endregion

    #region Set Tests

    /// <summary>
    /// Tests that the <c>Set</c> method correctly stores the given key-value pair in the cache.
    /// </summary>
    /// <remarks>
    /// This method validates the following:
    /// 1. The key-value pair is successfully added to the cache without any errors.
    /// 2. The stored value can be retrieved using the key.
    /// 3. The retrieved value matches the original value in terms of properties and data.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown when the assertions for successful storage or data integrity fail.
    /// </exception>
    [TestMethod]
    public void Set_WithValidData_StoresSuccessfully()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };

        // Act
        _cache.Set("key1", testData);

        // Assert
        bool result = _cache.TryGet("key1", out TestData retrievedData);
        Assert.IsTrue(result);
        Assert.AreEqual(testData.Id, retrievedData.Id);
        Assert.AreEqual(testData.Name, retrievedData.Name);
    }

    /// <summary>
    /// Verifies that calling the <see cref="Set"/> method with a null key
    /// throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided key is null.
    /// </exception>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Set_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };

        // Act
        _cache.Set(null!, testData);
    }

    /// Verifies that setting a null value in the cache stores the null value
    /// correctly and allows it to be retrieved afterwards.
    /// Test Steps:
    /// 1. A null value is stored in the cache using a specific key.
    /// 2. The value associated with the key is retrieved using the cache's TryGet method.
    /// 3. Validates that TryGet returns true, indicating the key exists in the cache.
    /// 4. Confirms that the retrieved value is null.
    /// This test ensures that the cache correctly handles scenarios where null
    /// values are explicitly stored and maintains consistent retrieval behavior.
    [TestMethod]
    public void Set_WithNullValue_StoresNull()
    {
        // Act
        _cache.Set("key1", null!);

        // Assert
        bool result = _cache.TryGet("key1", out TestData value);
        Assert.IsTrue(result);
        Assert.IsNull(value);
    }

    /// Verifies that setting a value with an existing key overwrites the previous value in the cache.
    /// Ensures that the updated value is correctly stored and can be retrieved using the specified key.
    /// This test method performs the following steps:
    /// 1. Adds an initial value associated with a key to the cache.
    /// 2. Overwrites the value associated with the same key.
    /// 3. Verifies that the updated value can be retrieved successfully.
    /// 4. Asserts that the retrieved data matches the updated value.
    [TestMethod]
    public void Set_OverwritingExistingKey_UpdatesValue()
    {
        // Arrange
        var testData1 = new TestData { Id = 1, Name = "Test1" };
        var testData2 = new TestData { Id = 2, Name = "Test2" };
        _cache.Set("key1", testData1);

        // Act
        _cache.Set("key1", testData2);

        // Assert
        bool result = _cache.TryGet("key1", out TestData retrievedData);
        Assert.IsTrue(result);
        Assert.AreEqual(testData2.Id, retrievedData.Id);
        Assert.AreEqual(testData2.Name, retrievedData.Name);
    }

    #endregion

    #region Remove Tests

    /// Validates that the `Remove` method successfully deletes an item from the cache
    /// and returns true when the provided key exists in the cache.
    /// This method sets up a cache with a specific key-value pair, then attempts to
    /// remove the key using the `Remove` method. It checks that:
    /// 1. `Remove` returns true to confirm the key was removed.
    /// 2. Subsequent attempts to retrieve the removed key return false.
    /// Preconditions:
    /// - A valid key-value pair is added to the cache before removal.
    /// Postconditions:
    /// - The key is no longer retrievable from the cache.
    /// - The cache indicates successful removal of the key.
    [TestMethod]
    public void Remove_WithExistingKey_ReturnsTrueAndRemovesItem()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };
        _cache.Set("key1", testData);

        // Act
        bool result = _cache.Remove("key1");

        // Assert
        Assert.IsTrue(result);
        bool getResult = _cache.TryGet("key1", out _);
        Assert.IsFalse(getResult);
    }

    /// Tests the behavior of the Remove method when attempting to remove a non-existent key
    /// from the FileCache. Ensures that the method handles the scenario correctly.
    /// Returns:
    /// False, indicating that the key was not found in the cache and no removal occurred.
    [TestMethod]
    public void Remove_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        bool result = _cache.Remove("nonexistent");

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that when an attempt is made to remove an item from the cache,
    /// and the associated file has already been deleted externally, the method
    /// returns <c>false</c> indicating that the removal was unsuccessful.
    /// Ensures that the cache can gracefully handle scenarios where the underlying
    /// file system has changed without the cache's knowledge.
    /// </summary>
    [TestMethod]
    public void Remove_WhenFileAlreadyDeleted_ReturnsFalse()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };
        _cache.Set("key1", testData);

        // Delete the file externally but keep the mapping
        string[] files = Directory.GetFiles(_tempDirectory, "*.json");
        string dataFile = files.FirstOrDefault(f => !f.EndsWith("_keymapping.json"));
        if (dataFile != null)
        {
            File.Delete(dataFile);
        }

        // Act
        bool result = _cache.Remove("key1");

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region Clear Tests

    /// <summary>
    /// Tests that when multiple items are present in the file cache, calling the <c>Clear</c> method successfully removes all items.
    /// </summary>
    /// <remarks>
    /// This test validates that all items are removed from the cache by performing the following steps:
    /// - Adds multiple items to the cache.
    /// - Calls the <c>Clear</c> method on the cache.
    /// - Asserts that retrieving any of the previously added items returns <c>false</c>.
    /// - Asserts that the count of keys in the cache is zero after clearing.
    /// </remarks>
    /// <seealso cref="FileCache{TKey, TValue}.Clear"/>
    [TestMethod]
    public void Clear_WithMultipleItems_RemovesAllItems()
    {
        // Arrange
        _cache.Set("key1", new TestData { Id = 1, Name = "Test1" });
        _cache.Set("key2", new TestData { Id = 2, Name = "Test2" });
        _cache.Set("key3", new TestData { Id = 3, Name = "Test3" });

        // Act
        _cache.Clear();

        // Assert
        Assert.IsFalse(_cache.TryGet("key1", out _));
        Assert.IsFalse(_cache.TryGet("key2", out _));
        Assert.IsFalse(_cache.TryGet("key3", out _));
        Assert.AreEqual(0, _cache.Keys().Count());
    }

    /// <summary>
    /// Verifies that calling the <see cref="FileCache{TKey, TValue}.Clear"/> method on an empty cache does not throw any exceptions.
    /// </summary>
    /// <remarks>
    /// This test ensures that the Clear method behaves gracefully and correctly handles situations where the cache is already empty, without throwing unexpected exceptions.
    /// </remarks>
    [TestMethod]
    public void Clear_WithEmptyCache_DoesNotThrow()
    {
        // Act & Assert
        _cache.Clear();
        Assert.AreEqual(0, _cache.Keys().Count());
    }

    #endregion

    #region Keys Tests

    /// <summary>
    /// Verifies that the Keys method returns an empty collection when the cache is empty.
    /// </summary>
    /// <remarks>
    /// Ensures the cache behaves correctly by returning an empty collection instead of null
    /// or throwing an exception when there are no entries in the cache.
    /// </remarks>
    [TestMethod]
    public void Keys_WithEmptyCache_ReturnsEmptyCollection()
    {
        // Act
        var keys = _cache.Keys();

        // Assert
        Assert.IsNotNull(keys);
        Assert.AreEqual(0, keys.Count());
    }

    /// Verifies that the `Keys` method in the `FileCache` class returns all keys
    /// currently stored in the cache when it contains multiple items.
    /// This test ensures that:
    /// - The `Keys` method accurately retrieves all keys present in the cache.
    /// - The returned collection has the correct number of elements.
    /// - The collection contains the expected keys.
    /// Test Steps:
    /// 1. Add multiple items to the cache using the `Set` method.
    /// 2. Call the `Keys` method to retrieve all stored keys.
    /// 3. Assert that the returned collection contains the expected number of keys.
    /// 4. Verify that all expected keys are present in the collection.
    [TestMethod]
    public void Keys_WithMultipleItems_ReturnsAllKeys()
    {
        // Arrange
        _cache.Set("key1", new TestData { Id = 1, Name = "Test1" });
        _cache.Set("key2", new TestData { Id = 2, Name = "Test2" });
        _cache.Set("key3", new TestData { Id = 3, Name = "Test3" });

        // Act
        var keys = _cache.Keys().ToList();

        // Assert
        Assert.AreEqual(3, keys.Count);
        Assert.IsTrue(keys.Contains("key1"));
        Assert.IsTrue(keys.Contains("key2"));
        Assert.IsTrue(keys.Contains("key3"));
    }

    #endregion

    #region IsAvailable Tests

    /// <summary>
    /// Validates that the IsAvailable method correctly returns true for a valid directory
    /// associated with the cache.
    /// </summary>
    /// <remarks>
    /// This test ensures that the cache reports its availability when the underlying directory
    /// is present and accessible.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown if the IsAvailable method does not return true for a valid directory.
    /// </exception>
    [TestMethod]
    public void IsAvailable_WithValidDirectory_ReturnsTrue()
    {
        // Act
        bool result = _cache.IsAvailable();

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies the behavior of the <see cref="FileCache{TKey, TValue}.IsAvailable"/> method
    /// when the cache is backed by a directory that has been deleted during execution.
    /// </summary>
    /// <remarks>
    /// This test ensures that the method correctly identifies the cache as unavailable
    /// when the backing directory is no longer present, and returns <c>false</c>.
    /// </remarks>
    /// <returns>
    /// Asserts <c>false</c> if the backing directory for the cache has been deleted.
    /// </returns>
    [TestMethod]
    public void IsAvailable_WithDeletedDirectory_ReturnsFalse()
    {
        // Arrange
        Directory.Delete(_tempDirectory, true);

        // Act
        bool result = _cache.IsAvailable();

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the IsAvailable method of the FileCache class returns false
    /// when the cache directory is set to a read-only attribute.
    /// </summary>
    /// <remarks>
    /// This test ensures that the IsAvailable method correctly considers file system permissions
    /// and is unable to operate when the directory is set to read-only. The test temporarily marks
    /// the directory as read-only, invokes the method, and checks the result.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown if the method does not return false as expected when the directory is read-only.
    /// </exception>
    [TestMethod]
    public void IsAvailable_WithReadOnlyDirectory_ReturnsFalse()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("ReadOnly attribute on directories does not prevent writes on Windows. Test skipped.");
            return;
        }
        
        // Arrange
        var dirInfo = new DirectoryInfo(_tempDirectory);
        dirInfo.Attributes |= FileAttributes.ReadOnly;

        try
        {
            // Act
            bool result = _cache.IsAvailable();

            // Assert
            Assert.IsFalse(result);
        }
        finally
        {
            // Cleanup
            dirInfo.Attributes &= ~FileAttributes.ReadOnly;
        }
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Validates that data stored in a <see cref="FileCache{TKey, TValue}"/> instance persists across multiple instance lifetimes
    /// when using the same underlying directory for storage.
    /// </summary>
    /// <remarks>
    /// This test confirms that data written to the cache is successfully retrievable after the original
    /// cache instance is disposed and a new instance is created using the same directory. It ensures that
    /// the file-based persistence mechanism works as expected, maintaining the integrity of cached data.
    /// </remarks>
    /// <exception cref="AssertFailedException">
    /// Thrown if the data is not found in the new cache instance or if the retrieved data does not match the original values.
    /// </exception>
    [TestMethod]
    public void FileCache_PersistenceAcrossInstances_MaintainsData()
    {
        // Arrange
        var testData = new TestData { Id = 1, Name = "Test" };
        _cache.Set("key1", testData);

        // Act - Create new instance with same directory
        var newCache = new FileCache<string, TestData>(_tempDirectory);
        bool result = newCache.TryGet("key1", out TestData retrievedData);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(retrievedData);
        Assert.AreEqual(testData.Id, retrievedData.Id);
        Assert.AreEqual(testData.Name, retrievedData.Name);
    }

    /// <summary>
    /// Validates that the FileCache implementation can handle complex keys correctly during
    /// operations such as storing, retrieving, and verifying data integrity.
    /// </summary>
    /// <remarks>
    /// This method tests the functionality of the FileCache class using a custom complex key
    /// consisting of multiple properties (e.g., Id and Category). It ensures that the cache
    /// can store and retrieve objects associated with these keys while maintaining data consistency.
    /// </remarks>
    /// <testcategory>
    /// Unit Test
    /// </testcategory>
    /// <seealso cref="FileCache{TKey, TValue}"/>
    [TestMethod]
    public void FileCache_WithComplexKeys_WorksCorrectly()
    {
        // Arrange
        var complexCache = new FileCache<ComplexKey, TestData>(_tempDirectory);
        var key = new ComplexKey { Id = 1, Category = "Test" };
        var testData = new TestData { Id = 1, Name = "Test" };

        // Act
        complexCache.Set(key, testData);
        bool result = complexCache.TryGet(key, out TestData retrievedData);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(retrievedData);
        Assert.AreEqual(testData.Id, retrievedData.Id);
        Assert.AreEqual(testData.Name, retrievedData.Name);
    }

    /// Tests the behavior of the FileCache when the mapping file is corrupted or contains invalid data.
    /// Verifies that the FileCache can handle the situation gracefully without throwing exceptions,
    /// initializes correctly, and operates normally, ensuring the cache is in a usable state after encountering a corrupted mapping file.
    [TestMethod]
    public void FileCache_WithCorruptedMappingFile_HandlesGracefully()
    {
        // Arrange
        string mappingFile = Path.Combine(_tempDirectory, "_keymapping.json");
        File.WriteAllText(mappingFile, "invalid json content");

        // Act - Create new instance that should handle corrupted mapping
        var newCache = new FileCache<string, TestData>(_tempDirectory);

        // Assert
        Assert.IsNotNull(newCache);
        Assert.AreEqual(0, newCache.Keys().Count());
    }

    #endregion
}

// Test data classes
/// <summary>
/// Represents the test data utilized in the context of cache and persistence testing.
/// </summary>
public class TestData
{
    /// <summary>
    /// Gets or sets the unique identifier for the data instance.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name associated with the test data instance.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Represents a complex key consisting of multiple properties, used for caching scenarios.
/// </summary>
public class ComplexKey
{
    /// Represents the unique identifier of an entity or object.
    /// This property is commonly used as a key to differentiate between instances.
    /// It is essential for ensuring equality and proper functionality in dictionary-based or hash-based collections.
    public int Id { get; set; }

    /// <summary>
    /// Represents the category or classification of a complex key.
    /// This property is used to group or differentiate data items
    /// within a caching mechanism based on their classifications.
    /// </summary>
    public string? Category { get; set; }

    /// Determines whether the specified object is equal to the current object.
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ComplexKey other && Id == other.Id && Category == other.Category;
    }

    /// Returns the hash code for the current object.
    /// This method is overridden to provide a hash code based on the values of the object's properties,
    /// ensuring a consistent and unique hash for objects with identical property values.
    /// <return>Returns an integer representation of the combined hash code for the object's properties.</return>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Category);
    }
}

// Simple test logger implementation
/// <summary>
/// Implementation of a simple <see cref="ILogger{T}"/> used for testing purposes.
/// </summary>
/// <typeparam name="T">The type associated with the logger instance.</typeparam>
public class TestLogger<T> : ILogger<T>
{
    /// Starts a logical operation scope.
    /// <param name="state">The identifier for the scope. This is used to associate log messages with a specific operation or context.</param>
    /// <typeparam name="TState">The type of the state parameter, which must be non-nullable.</typeparam>
    /// <returns>An IDisposable object that ends the logical operation scope on disposal, or null if no action is required.</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// Determines if a specific log level is enabled for logging.
    /// <param name="logLevel">The log level to check for availability.</param>
    /// <returns>True if the specified log level is enabled; otherwise, false.</returns>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// Logs a message with a specified log level, event ID, state, exception, and formatter function.
    /// <param name="logLevel">
    /// The log severity level (e.g., Information, Warning, Error).
    /// </param>
    /// <param name="eventId">
    /// An identifier for the log event.
    /// </param>
    /// <param name="state">
    /// The state to be logged, typically containing the log entry data.
    /// </param>
    /// <param name="exception">
    /// An optional exception associated with the log entry, if applicable.
    /// </param>
    /// <param name="formatter">
    /// A function that formats the state and exception for the log entry.
    /// </param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}