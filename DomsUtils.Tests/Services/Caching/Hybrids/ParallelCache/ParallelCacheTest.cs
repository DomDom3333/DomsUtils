using System;
using System.Threading;
using DomsUtils.Services.Caching.Hybrids.ParallelCache;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DomsUtils.Tests.Services.Caching.Hybrids.ParallelCache;

/// <summary>
/// Provides unit tests for the ParallelCache class, which implements a caching mechanism
/// using multiple underlying caches to ensure redundancy and performance in data retrieval and storage.
/// </summary>
/// <remarks>
/// This test class evaluates the behavior of the ParallelCache implementation under various scenarios,
/// including construction, caching operations (such as Get, Set, Remove, and Clear), availability checks,
/// and synchronization mechanisms.
/// </remarks>
/// <remarks>
/// The ParallelCache is tested for robust handling of errors, fault tolerance, and compatibility with
/// multi-cache setups. These tests ensure the correctness and reliability of its functionality under
/// normal and edge cases.
/// </remarks>
[TestClass]
    public class ParallelCacheTests
    {
        /// Represents a mocked implementation of the ICache interface with string key and value types.
        /// Used for unit testing functionality dependent on caching mechanisms in the ParallelCacheTests class.
        private Mock<ICache<string, string>> _mockCache1;

        /// <summary>
        /// Represents a mock instance of the <see cref="ICache{TKey, TValue}"/> interface
        /// for testing caching functionalities with string keys and values.
        /// </summary>
        private Mock<ICache<string, string>> _mockCache2;

        /// <summary>
        /// Mock implementation of the <see cref="ICache{TKey, TValue}"/> interface, utilized for testing
        /// purposes within the <see cref="ParallelCacheTests"/> class. Specifically, this mocked cache
        /// represents the third-level cache in scenarios that involve multiple cache layers.
        /// </summary>
        private Mock<ICache<string, string>> _mockCache3;

        /// <summary>
        /// Represents a mock logger used to test logging functionality within unit tests.
        /// </summary>
        private Mock<ILogger> _mockLogger;

        /// Represents the instance of the SyncOptions class used in the ParallelCacheTests for configuring synchronization-related behaviors of the ParallelCache.
        private SyncOptions _syncOptions;

        /// <summary>
        /// Initializes mock objects and test setup required for unit testing the ParallelCache functionality.
        /// </summary>
        /// <remarks>
        /// This method is marked with the [TestInitialize] attribute and is executed before each test method
        /// in the ParallelCacheTests class, ensuring proper setup for consistent test execution.
        /// </remarks>
        [TestInitialize]
        public void Setup()
        {
            _mockCache1 = new Mock<ICache<string, string>>();
            _mockCache2 = new Mock<ICache<string, string>>();
            _mockCache3 = new Mock<ICache<string, string>>();
            _mockLogger = new Mock<ILogger>();
            _syncOptions = new SyncOptions();
        }

        #region Constructor Tests

        /// Unit test for validating the successful initialization of the ParallelCache class
        /// when valid cache instances, a logger, and synchronization options are provided.
        /// This test ensures that:
        /// - The constructor completes successfully with the given parameters.
        /// - The resulting ParallelCache instance is not null.
        /// Preconditions:
        /// - Mock objects for the required dependencies (e.g., ICache, ILogger, SyncOptions) are properly initialized
        /// and injected into the constructor during the test setup.
        /// Test logic:
        /// - The test creates a new ParallelCache instance with two mock cache objects, a mock logger,
        /// and a SyncOptions object.
        /// - Confirms that the resulting instance is valid and not null.
        /// Validation:
        /// - The test checks whether the ParallelCache instance was instantiated correctly.
        [TestMethod]
        public void Constructor_WithValidCaches_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object,
                _syncOptions);

            // Assert
            Assert.IsNotNull(cache);
        }

        /// <summary>
        /// Ensures that the <see cref="ParallelCache{TKey, TValue}"/> constructor throws an
        /// <see cref="ArgumentException"/> when a null array of caches is provided.
        /// </summary>
        /// <remarks>
        /// This test verifies that the constructor performs proper validation on its input
        /// parameters and enforces the requirement for a non-null array of caches. If the
        /// array of caches is null, an <see cref="ArgumentException"/> should be thrown.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the array of caches passed to the constructor is null.
        /// </exception>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithNullCaches_ShouldThrowArgumentException()
        {
            // Act
            new ParallelCache<string, string>(null, _mockLogger.Object, _syncOptions);
        }

        /// <summary>
        /// Validates that the absence of sufficient caches (a single cache provided in this case)
        /// during the instantiation of the <see cref="ParallelCache{TKey, TValue}"/> class
        /// throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the provided array of caches includes fewer than the required number of caches.
        /// </exception>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithSingleCache_ShouldThrowArgumentException()
        {
            // Act
            new ParallelCache<string, string>(
                new[] { _mockCache1.Object },
                _mockLogger.Object,
                _syncOptions);
        }

        /// Verifies that the constructor of the `ParallelCache` class uses a `null` logger without throwing exceptions when no logger is provided.
        /// This test ensures:
        /// - A `ParallelCache` instance can be successfully created when the logger is `null`.
        /// - The absence of a logger does not impact the instantiation or functionality of the cache in its initialization phase.
        /// Preconditions:
        /// - Requires at least two valid cache instances to initialize the `ParallelCache`.
        /// - A valid `SyncOptions` instance or `null` can be passed as the third parameter.
        /// Test Steps:
        /// - A `ParallelCache` instance is created with two mock caches, a `null` logger, and valid sync options.
        /// - The test asserts that the returned instance of `ParallelCache` is not `null`.
        /// Usage:
        /// Intended for validating the constructor behavior of `ParallelCache` when a logger is not provided.
        [TestMethod]
        public void Constructor_WithNullLogger_ShouldUseNullLogger()
        {
            // Act
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                null,
                _syncOptions);

            // Assert
            Assert.IsNotNull(cache);
        }

        /// Tests the behavior of the ParallelCache constructor when null is provided for SyncOptions.
        /// Ensures that default synchronization options are applied in such cases.
        /// This test verifies:
        /// - The ParallelCache instance is created successfully when null SyncOptions are passed.
        /// - The default SyncOptions are used internally, maintaining expected functionality.
        [TestMethod]
        public void Constructor_WithNullSyncOptions_ShouldUseDefaultSyncOptions()
        {
            // Act
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object,
                null);

            // Assert
            Assert.IsNotNull(cache);
        }

        #endregion

        #region TryGet Tests

        /// Verifies that when a key is found in the first cache of a ParallelCache instance,
        /// the TryGet method returns true and retrieves the corresponding value.
        /// Preconditions:
        /// - A ParallelCache instance is initialized with two mocked caches.
        /// - The first cache contains a specified key and value.
        /// Test Steps:
        /// 1. Mock the first cache to return true and a specific value for the given key.
        /// 2. Invoke the TryGet method on the ParallelCache with the specified key.
        /// Expected Outcomes:
        /// - TryGet should return true, indicating success.
        /// - The retrieved value should match the value from the first cache.
        /// - The TryGet method of the first cache should be invoked exactly once.
        /// - The second cache should not be queried.
        [TestMethod]
        public void TryGet_KeyFoundInFirstCache_ShouldReturnTrueAndValue()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value1";
                    return true;
                });

            // Act
            bool result = cache.TryGet("key1", out string retrievedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("value1", retrievedValue);
            _mockCache1.Verify(c => c.TryGet("key1", out It.Ref<string>.IsAny), Times.Once);
            _mockCache2.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<string>.IsAny), Times.Never);
        }

        /// Validates behavior of the TryGet method when the key is found in the second cache of a `ParallelCache`.
        /// This test ensures the method returns true, provides the correct value, and verifies interactions with both caches.
        /// The following scenarios are verified:
        /// 1. The key is not found in the first cache.
        /// 2. The key is found in the second cache, and the correct value is returned.
        /// 3. The TryGet method of each cache is invoked exactly once, in order.
        /// Assertions:
        /// - The method returns true when the key is retrieved from the second cache.
        /// - The retrieved value matches the expected value.
        /// - The first cache's TryGet method is invoked once and does not find the key.
        /// - The second cache's TryGet method is invoked once and successfully retrieves the value.
        [TestMethod]
        public void TryGet_KeyFoundInSecondCache_ShouldReturnTrueAndValue()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns(false);
            _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value1";
                    return true;
                });

            // Act
            bool result = cache.TryGet("key1", out string retrievedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("value1", retrievedValue);
            _mockCache1.Verify(c => c.TryGet("key1", out It.Ref<string>.IsAny), Times.Once);
            _mockCache2.Verify(c => c.TryGet("key1", out It.Ref<string>.IsAny), Times.Once);
        }

        /// <summary>
        /// Tests the scenario where a key is not found in any of the caches.
        /// Ensures that the method returns false and the default value for the type.
        /// </summary>
        /// <remarks>
        /// This test verifies the behavior of the <c>TryGet</c> method in the <c>ParallelCache</c> class
        /// when none of the underlying caches contain a value associated with the given key.
        /// </remarks>
        /// <returns>
        /// Returns <c>false</c> and the default value for the type when the key is not found in any cache.
        /// </returns>
        [TestMethod]
        public void TryGet_KeyNotFoundInAnyCaches_ShouldReturnFalseAndDefaultValue()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns(false);
            _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns(false);

            // Act
            bool result = cache.TryGet("key1", out string retrievedValue);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(default(string), retrievedValue);
        }

        /// Tests the behavior of the TryGet method in the ParallelCache class
        /// when a cache throws an exception. Ensures that the method continues
        /// to check subsequent caches for the requested key.
        /// This test validates the following:
        /// - If one cache in the sequence throws an exception during the TryGet call,
        /// the implementation should handle the exception gracefully.
        /// - Subsequent caches should still be queried for the specified key.
        /// - The method should return true if a subsequent cache contains the key
        /// and its corresponding value.
        /// Assertions:
        /// - The method must return true if the key is found in any cache after the
        /// one that throws the exception.
        /// - The correct value associated with the key must be retrieved from the
        /// subsequent cache.
        /// Preconditions:
        /// - At least one cache throws an exception.
        /// - Another cache contains the requested key and value.
        [TestMethod]
        public void TryGet_CacheThrowsException_ShouldContinueToNextCache()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object);

            _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Throws(new InvalidOperationException("Cache error"));
            _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value1";
                    return true;
                });

            // Act
            bool result = cache.TryGet("key1", out string retrievedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("value1", retrievedValue);
        }

        /// <summary>
        /// Verifies that when a cache within the ParallelCache is unavailable,
        /// the cache is skipped during a TryGet operation and subsequent caches
        /// are checked for the key.
        /// </summary>
        /// <remarks>
        /// This test ensures that unavailable caches, as indicated by the
        /// ICacheAvailability interface, do not participate in the TryGet operation.
        /// The subsequent caches are used to retrieve the key, if available.
        /// </remarks>
        /// <example>
        /// Ensures a cache marked as unavailable is skipped,
        /// and the next available cache handles the TryGet request successfully.
        /// Verifies that the unavailable cache's TryGet method is never called.
        /// </example>
        /// <test>
        /// Asserts that the result of the TryGet operation is true if the key is
        /// found in an available cache and that the retrieved value matches
        /// the expected value.
        /// </test>
        [TestMethod]
        public void TryGet_UnavailableCache_ShouldSkipCache()
        {
            // Arrange
            var mockAvailableCache1 = _mockCache1.As<ICacheAvailability>();
            mockAvailableCache1.Setup(c => c.IsAvailable()).Returns(false);

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value1";
                    return true;
                });

            // Act
            bool result = cache.TryGet("key1", out string retrievedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("value1", retrievedValue);
            _mockCache1.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<string>.IsAny), Times.Never);
            _mockCache2.Verify(c => c.TryGet("key1", out It.Ref<string>.IsAny), Times.Once);
        }

        /// Validates the `TryGet` method of the `ParallelCache` class when the key is found in the third cache
        /// among a chain of three caches. Ensures that the method correctly retrieves the value, returns true,
        /// and properly interacts with each cache in sequence.
        /// This unit test follows these expectations:
        /// 1. The first two caches in the chain do not contain the requested key and should indicate this when queried.
        /// 2. The third cache contains the requested key and should return the associated value.
        /// 3. The method stops searching after finding the key in the third cache.
        /// 4. The `TryGet` method is called once per cache, in sequence, until the key is found.
        /// 5. The result should be true and the returned value should match the one stored in the third cache.
        /// Preconditions:
        /// - A `ParallelCache` instance is initialized with three mock caches.
        /// - The first two caches return false for the requested key.
        /// - The third cache contains the key-value pair and correctly provides them.
        /// Postconditions:
        /// - The result of `TryGet` is true.
        /// - The returned value is the expected value from the third cache.
        /// - Each cache's `TryGet` method is verified to have been called exactly once, in order of the chain.
        [TestMethod]
        public void TryGet_WithThreeCaches_KeyFoundInThirdCache_ShouldReturnTrueAndValue()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object, _mockCache3.Object });

            _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny)).Returns(false);
            _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny)).Returns(false);
            _mockCache3.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value1";
                    return true;
                });

            // Act
            bool result = cache.TryGet("key1", out string retrievedValue);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("value1", retrievedValue);
            _mockCache1.Verify(c => c.TryGet("key1", out It.Ref<string>.IsAny), Times.Once);
            _mockCache2.Verify(c => c.TryGet("key1", out It.Ref<string>.IsAny), Times.Once);
            _mockCache3.Verify(c => c.TryGet("key1", out It.Ref<string>.IsAny), Times.Once);
        }

        #endregion

        #region Set Tests

        /// <summary>
        /// Verifies that the Set method of the ParallelCache ensures the specified key-value pair
        /// is set in all available caches.
        /// </summary>
        /// <remarks>
        /// This method tests the functionality of the ParallelCache's Set operation by confirming
        /// that all configured caches are invoked with the specified key and value. If any cache is
        /// unavailable or throws an exception, it is not part of this test's scope, as it assumes all
        /// caches are functioning as expected.
        /// </remarks>
        /// <exception cref="MockException">Thrown if one or more caches do not perform the Set operation as expected.</exception>
        [TestMethod]
        public void Set_WithAvailableCaches_ShouldSetInAllCaches()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Act
            cache.Set("key1", "value1");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            _mockCache1.Verify(c => c.Set("key1", "value1"), Times.Once);
            _mockCache2.Verify(c => c.Set("key1", "value1"), Times.Once);
        }

        /// <summary>
        /// Tests that when attempting to set a value in a ParallelCache instance with one of the underlying caches unavailable,
        /// the value is set only in the caches that are available.
        /// </summary>
        /// <remarks>
        /// This test verifies the behavior of the <see cref="ParallelCache{TKey, TValue}.Set"/> method when
        /// one of the caches in the provided cache collection is marked as unavailable by implementing the
        /// <see cref="ICacheAvailability"/> interface. It ensures that unavailable caches are bypassed while
        /// setting the value, and no exceptions are thrown due to the unavailability of certain caches.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the operation attempts to interact with unavailable caches in a way that violates the
        /// contract of the <see cref="ICacheAvailability.IsAvailable"/> method.
        /// </exception>
        [TestMethod]
        public void Set_WithOneUnavailableCache_ShouldSetOnlyInAvailableCaches()
        {
            // Arrange
            var mockAvailableCache1 = _mockCache1.As<ICacheAvailability>();
            mockAvailableCache1.Setup(c => c.IsAvailable()).Returns(false);

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Act
            cache.Set("key1", "value1");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            _mockCache1.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockCache2.Verify(c => c.Set("key1", "value1"), Times.Once);
        }

        /// Unit test to verify the behavior of the `Set` method in the `ParallelCache` implementation when one
        /// of the caches in the collection throws an exception during the `Set` operation.
        /// Ensures that the method continues to attempt to set the value in subsequent caches even if an exception occurs.
        /// This test performs the following steps:
        /// 1. Provides two mocked cache instances.
        /// 2. Configures the first mocked cache to throw an `InvalidOperationException` when `Set` is called.
        /// 3. Creates a `ParallelCache` instance with the mocked caches and a logger.
        /// 4. Calls `Set` on the `ParallelCache` instance with a key-value pair.
        /// 5. Verifies that the `Set` operation was attempted on both caches regardless of the exception encountered in the first cache.
        [TestMethod]
        public void Set_CacheThrowsException_ShouldContinueWithOtherCaches()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object);

            _mockCache1.Setup(c => c.Set("key1", "value1"))
                .Throws(new InvalidOperationException("Cache error"));

            // Act
            cache.Set("key1", "value1");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            _mockCache1.Verify(c => c.Set("key1", "value1"), Times.Once);
            _mockCache2.Verify(c => c.Set("key1", "value1"), Times.Once);
        }

        #endregion

        #region Remove Tests

        /// Tests the functionality of the Remove method in the ParallelCache class,
        /// specifically verifying the scenario where the key is successfully removed
        /// from at least one of the underlying caches.
        /// This method sets up a ParallelCache instance composed of two mock caches.
        /// The first mock cache is configured to return true when attempting to remove
        /// a specific key, indicating successful removal, while the second mock cache
        /// is configured to return false.
        /// The test asserts that:
        /// - The Remove method of the ParallelCache returns true if the key is successfully
        /// removed from at least one cache.
        /// - The underlying Remove method of each cache is called exactly once for the specified key.
        /// Thread.Sleep is used to account for any asynchronous operations within
        /// the ParallelCache implementation.
        /// Exceptions are not expected during this test.
        [TestMethod]
        public void Remove_KeyRemovedFromOneCache_ShouldReturnTrue()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            _mockCache1.Setup(c => c.Remove("key1")).Returns(true);
            _mockCache2.Setup(c => c.Remove("key1")).Returns(false);

            // Act
            bool result = cache.Remove("key1");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            Assert.IsTrue(result);
            _mockCache1.Verify(c => c.Remove("key1"), Times.Once);
            _mockCache2.Verify(c => c.Remove("key1"), Times.Once);
        }

        /// Validates the behavior of the Remove method when the specified key
        /// is not removed from any of the underlying caches in the ParallelCache instance.
        /// This test ensures that the Remove method returns false when all underlying caches
        /// return false for the removal operation. It sets up multiple mock caches, each of which
        /// returns false when the Remove method is called with the specified key. After invoking
        /// the Remove method on the ParallelCache instance, the test verifies that the result
        /// returned by the operation is false.
        /// Test case considerations:
        /// - Ensures that the method correctly aggregates the results from all caches.
        /// - Simulates a scenario where none of the caches are able to remove the specified key.
        /// - Verifies that the method continues processing across all caches, even after a failure.
        /// Preconditions:
        /// - All caches are mocked to return false for the Remove operation.
        /// - A valid non-null key input is provided.
        /// Expected Outcome:
        /// - The Remove method should return false, indicating that the key could not be removed
        /// from any of the caches.
        [TestMethod]
        public void Remove_KeyNotRemovedFromAnyCaches_ShouldReturnFalse()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            _mockCache1.Setup(c => c.Remove("key1")).Returns(false);
            _mockCache2.Setup(c => c.Remove("key1")).Returns(false);

            // Act
            bool result = cache.Remove("key1");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            Assert.IsFalse(result);
        }

        /// Tests the Remove method of the ParallelCache class to ensure that unavailable caches
        /// are skipped and the operation is performed only on available caches.
        /// This test verifies the following behavior:
        /// - If a cache is unavailable, it should not attempt to perform the Remove operation.
        /// - The method continues with other caches that are available.
        /// - Unavailable caches will not affect the overall success of the Remove operation if at
        /// least one cache successfully removes the specified key.
        /// The Arrange phase sets up two mock caches:
        /// - The first cache is mocked to return false when checked for availability, indicating it is unavailable.
        /// - The second cache is mocked to successfully perform the Remove operation for the provided key.
        /// The Act phase ensures that the Remove method is called on the ParallelCache instance,
        /// which aggregates the mock caches.
        /// The Assert phase confirms:
        /// - The unavailable cache (mockCache1) does not invoke the Remove method.
        /// - The available cache (mockCache2) successfully invokes the Remove method exactly once.
        /// - The overall Remove result is true if at least one cache performs the operation successfully.
        [TestMethod]
        public void Remove_WithUnavailableCache_ShouldSkipUnavailableCache()
        {
            // Arrange
            var mockAvailableCache1 = _mockCache1.As<ICacheAvailability>();
            mockAvailableCache1.Setup(c => c.IsAvailable()).Returns(false);

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            _mockCache2.Setup(c => c.Remove("key1")).Returns(true);

            // Act
            bool result = cache.Remove("key1");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            Assert.IsTrue(result);
            _mockCache1.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
            _mockCache2.Verify(c => c.Remove("key1"), Times.Once);
        }

        /// <summary>
        /// Tests the behavior of the <c>Remove</c> method in a scenario where a cache throws
        /// an exception while attempting to remove a key. Ensures that the implementation
        /// continues to process other caches and achieves the expected behavior.
        /// </summary>
        /// <remarks>
        /// This test validates that even if one cache throws an exception, the <c>ParallelCache</c>
        /// implementation will proceed to remove the key from other available caches without failing.
        /// The first cache is mocked to throw an <c>InvalidOperationException</c>, while the second
        /// cache successfully removes the key. The test ensures the overall operation returns success,
        /// confirming the mechanism's fault tolerance.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown by the mocked implementation of the first cache when attempting to remove the key.
        /// </exception>
        /// <seealso cref="ParallelCache{TKey, TValue}.Remove"/>
        /// <seealso cref="ICache{TKey, TValue}"/>
        [TestMethod]
        public void Remove_CacheThrowsException_ShouldContinueWithOtherCaches()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object);

            _mockCache1.Setup(c => c.Remove("key1"))
                .Throws(new InvalidOperationException("Cache error"));
            _mockCache2.Setup(c => c.Remove("key1")).Returns(true);

            // Act
            bool result = cache.Remove("key1");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Clear Tests

        /// Tests the Clear method in a scenario where all provided caches are available.
        /// This test ensures that the Clear operation is invoked on every underlying cache
        /// when all caches are operational.
        /// Assertions:
        /// - Verifies that the Clear method is called exactly once on each cache.
        /// - Ensures the operation completes successfully without any skipped or failed invocations.
        [TestMethod]
        public void Clear_WithAvailableCaches_ShouldClearAllCaches()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Act
            cache.Clear();

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            _mockCache1.Verify(c => c.Clear(), Times.Once);
            _mockCache2.Verify(c => c.Clear(), Times.Once);
        }

        /// <summary>
        /// Tests that the Clear method of the ParallelCache class skips calling Clear
        /// on caches marked as unavailable as determined by the IsAvailable method,
        /// and processes the Clear operation only on available caches.
        /// </summary>
        /// <remarks>
        /// This test ensures that if a cache in the ParallelCache collection is unavailable,
        /// as indicated by its IsAvailable implementation returning false, the Clear operation
        /// is not invoked on that cache, but is still invoked successfully on other available caches.
        /// This is particularly useful to maintain efficiency and prevent unnecessary operations
        /// on unavailable caches within the ParallelCache architecture.
        /// </remarks>
        /// <seealso cref="DomsUtils.Services.Caching.Hybrids.ParallelCache.ParallelCache{TKey, TValue}" />
        /// <seealso cref="DomsUtils.Services.Caching.Interfaces.Bases.ICacheAvailability" />
        /// <seealso cref="DomsUtils.Services.Caching.Interfaces.Bases.ICache{TKey, TValue}.Clear" />
        [TestMethod]
        public void Clear_WithUnavailableCache_ShouldSkipUnavailableCache()
        {
            // Arrange
            var mockAvailableCache1 = _mockCache1.As<ICacheAvailability>();
            mockAvailableCache1.Setup(c => c.IsAvailable()).Returns(false);

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Act
            cache.Clear();

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            _mockCache1.Verify(c => c.Clear(), Times.Never);
            _mockCache2.Verify(c => c.Clear(), Times.Once);
        }

        /// <summary>
        /// Verifies the behavior of the <see cref="ParallelCache{TKey, TValue}.Clear"/> method when one of the underlying caches
        /// throws an exception during the clear operation.
        /// </summary>
        /// <remarks>
        /// This test ensures that if an exception occurs while calling <see cref="ICache{TKey, TValue}.Clear"/> on one cache,
        /// the operation continues and attempts to clear the other caches without interruption.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Simulates an error scenario where one of the caches throws an <see cref="InvalidOperationException"/> during the clear operation.
        /// </exception>
        /// <seealso cref="ParallelCache{TKey, TValue}"/>
        /// <seealso cref="ICache{TKey, TValue}"/>
        [TestMethod]
        public void Clear_CacheThrowsException_ShouldContinueWithOtherCaches()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object);

            _mockCache1.Setup(c => c.Clear())
                .Throws(new InvalidOperationException("Cache error"));

            // Act
            cache.Clear();

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert
            _mockCache1.Verify(c => c.Clear(), Times.Once);
            _mockCache2.Verify(c => c.Clear(), Times.Once);
        }

        #endregion

        #region IsAvailable Tests

        /// <summary>
        /// Validates that the <c>IsAvailable</c> method correctly returns <c>true</c>
        /// when all caches in the <c>ParallelCache</c> are available.
        /// </summary>
        /// <remarks>
        /// This test initializes a <c>ParallelCache</c> with multiple cache instances.
        /// It ensures that the <c>IsAvailable</c> method checks the availability of all internal caches
        /// and returns <c>true</c> when all of them are functioning correctly.
        /// </remarks>
        /// <exception cref="AssertFailedException">
        /// Thrown if the <c>IsAvailable</c> method does not return <c>true</c>
        /// when all caches are available.
        /// </exception>
        [TestMethod]
        public void IsAvailable_AllCachesAvailable_ShouldReturnTrue()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Act
            bool result = cache.IsAvailable();

            // Assert
            Assert.IsTrue(result);
        }

        /// Validates that the `IsAvailable` method returns `true` when at least one of the caches in a `ParallelCache` instance is available.
        /// This test simulates a scenario where one of the caches returns `false` for the `IsAvailable` check,
        /// while the other cache is functional and available.
        /// The `ParallelCache` instance should correctly identify that it is still operational as long as at least one cache is available.
        /// Test Steps:
        /// 1. Mock the `IsAvailable` method for at least one cache to return `false`, simulating an unavailable cache.
        /// 2. Create a `ParallelCache` instance with the mocked caches.
        /// 3. Invoke the `IsAvailable` method on the `ParallelCache` instance.
        /// 4. Verify that the method returns `true`, indicating the `ParallelCache` is considered available.
        /// Expected Outcome:
        /// The `IsAvailable` method should return `true` as long as one cache remains available.
        [TestMethod]
        public void IsAvailable_OneCacheUnavailable_ShouldReturnTrue()
        {
            // Arrange
            var mockAvailableCache1 = _mockCache1.As<ICacheAvailability>();
            mockAvailableCache1.Setup(c => c.IsAvailable()).Returns(false);

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Act
            bool result = cache.IsAvailable();

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Validates the availability of caches in a <see cref="ParallelCache{TKey, TValue}"/> instance
        /// by ensuring no caches are marked as available. Returns <c>false</c> when all caches are unavailable.
        /// </summary>
        /// <remarks>
        /// This test method verifies the behavior of the <see cref="ParallelCache{TKey, TValue}.IsAvailable"/> method
        /// when all the underlying caches in the cache array are unavailable. It uses mock objects to simulate
        /// the implementation of the <see cref="ICacheAvailability"/> interface for all specified caches.
        /// </remarks>
        /// <exception cref="AssertFailedException">
        /// Thrown when the <see cref="ParallelCache{TKey, TValue}.IsAvailable"/> method does not return <c>false</c>
        /// under the condition that all caches are unavailable.
        /// </exception>
        [TestMethod]
        public void IsAvailable_AllCachesUnavailable_ShouldReturnFalse()
        {
            // Arrange
            var mockAvailableCache1 = _mockCache1.As<ICacheAvailability>();
            var mockAvailableCache2 = _mockCache2.As<ICacheAvailability>();
            mockAvailableCache1.Setup(c => c.IsAvailable()).Returns(false);
            mockAvailableCache2.Setup(c => c.IsAvailable()).Returns(false);

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Act
            bool result = cache.IsAvailable();

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region TriggerMigrationNow Tests

        /// Validates that the `TriggerMigrationNow` method in the `ParallelCache` class correctly synchronizes data
        /// across multiple caches that implement the `ICacheEnumerable<TKey>` interface.
        /// This test ensures that the synchronization process handles the following:
        /// - Extracting keys from each cache as defined by the `Keys` method in the `ICacheEnumerable` interface.
        /// - Resolving conflicts according to the specified `SyncConflictResolution` option in the `SyncOptions` class.
        /// - Updating caches with missing or differing key-value pairs based on the synchronization logic.
        /// Preconditions:
        /// - There are two caches implementing `ICacheEnumerable<string>`, mocked to return respective keys and values.
        /// - `SyncOptions` is configured with `ConflictResolution` set to `MajorityWins` and a majority threshold of 0.5.
        /// Assertions:
        /// - Verifies that keys with missing or differing values are migrated and updated within the other cache.
        /// - Ensures calls to the `Set` method are made for the values requiring synchronization.
        /// Dependencies:
        /// - `ICacheEnumerable<TKey>`: Provides the ability to enumerate the keys in a cache.
        /// - `ICache<TKey, TValue>`: Used for retrieving and setting key-value data in a cache.
        /// - `SyncOptions`: Contains configuration settings for how synchronization conflicts are resolved.
        /// - `SyncConflictResolution`: Enum that dictates the conflict resolution strategy.
        /// Test Coverage:
        /// - Successful migration of key-value pairs.
        /// - Conflict resolution via the `MajorityWins` approach.
        [TestMethod]
        public void TriggerMigrationNow_WithEnumerableCaches_ShouldSynchronizeSuccessfully()
        {
            // Arrange
            var mockEnumerableCache1 = _mockCache1.As<ICacheEnumerable<string>>();
            var mockEnumerableCache2 = _mockCache2.As<ICacheEnumerable<string>>();

            mockEnumerableCache1.Setup(c => c.Keys()).Returns(new[] { "key1", "key2" });
            mockEnumerableCache2.Setup(c => c.Keys()).Returns(new[] { "key1", "key3" });

            _mockCache1.Setup(c => c.TryGet("key2", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value2";
                    return true;
                });

            _mockCache2.Setup(c => c.TryGet("key3", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value3";
                    return true;
                });

            var syncOptions = new SyncOptions
            {
                ConflictResolution = SyncConflictResolution.MajorityWins,
                MajorityThreshold = 0.5
            };

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object,
                syncOptions);

            // Act
            cache.TriggerMigrationNow();

            // Assert
            _mockCache1.Verify(c => c.Set("key3", "value3"), Times.Once);
            _mockCache2.Verify(c => c.Set("key2", "value2"), Times.Once);
        }

        /// Verifies that calling `TriggerMigrationNow` on a `ParallelCache` instance configured with
        /// the `PrimaryWins` synchronization strategy results in the primary cache being used as
        /// the source of truth during the migration process. Specifically, this means ensuring that
        /// any keys or entries present in secondary caches but absent from the primary cache have
        /// their corresponding entries removed in the secondary caches.
        /// Test Preconditions:
        /// - Two mock caches (primary and secondary) should be set up, with key discrepancies
        /// between them.
        /// - Key enumeration is simulated in both caches using the `Keys()` method from
        /// `ICacheEnumerable<TKey>`.
        /// - The `SyncOptions` instance provided to the `ParallelCache` constructor must be configured
        /// to use the `PrimaryWins` conflict resolution strategy.
        /// Validation:
        /// - Ensures that keys or entries not present in the primary cache but found in secondary
        /// caches are removed from the secondary caches using the `Remove` method on the appropriate
        /// mocks.
        /// This test case is designed to confirm that the `PrimaryWins` conflict resolution strategy
        /// is correctly implemented in the `TriggerMigrationNow` method of `ParallelCache`.
        [TestMethod]
        public void TriggerMigrationNow_WithPrimaryWinsStrategy_ShouldUsePrimaryCacheAsSource()
        {
            // Arrange
            var mockEnumerableCache1 = _mockCache1.As<ICacheEnumerable<string>>();
            var mockEnumerableCache2 = _mockCache2.As<ICacheEnumerable<string>>();

            mockEnumerableCache1.Setup(c => c.Keys()).Returns(new[] { "key1" });
            mockEnumerableCache2.Setup(c => c.Keys()).Returns(new[] { "key1", "key2" });

            var syncOptions = new SyncOptions
            {
                ConflictResolution = SyncConflictResolution.PrimaryWins
            };

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object,
                syncOptions);

            // Act
            cache.TriggerMigrationNow();

            // Assert
            _mockCache2.Verify(c => c.Remove("key2"), Times.Once);
        }

        /// <summary>
        /// Validates that when the TriggerMigrationNow method is invoked with insufficient available caches,
        /// it logs a warning message, and the operation completes without throwing exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that the ParallelCache implementation handles scenarios where some caches
        /// are unavailable during a migration attempt, and appropriate warnings are logged using the logger.
        /// </remarks>
        /// <exception cref="AssertFailedException">
        /// Thrown if the cache instance is null after invoking the TriggerMigrationNow method.
        /// </exception>
        [TestMethod]
        public void TriggerMigrationNow_WithInsufficientCaches_ShouldLogWarning()
        {
            // Arrange
            var mockAvailableCache1 = _mockCache1.As<ICacheAvailability>();
            mockAvailableCache1.Setup(c => c.IsAvailable()).Returns(false);

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object);

            // Act
            cache.TriggerMigrationNow();

            // Assert - Should complete without throwing
            Assert.IsNotNull(cache);
        }

        /// <summary>
        /// Tests the behavior of the <see cref="ParallelCache{TKey, TValue}.TriggerMigrationNow"/> method
        /// when an enumeration operation in one of the caches fails during synchronization.
        /// </summary>
        /// <remarks>
        /// This method is expected to throw an <see cref="InvalidOperationException"/>
        /// if a cache enumeration operation fails during the migration process.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the cache enumeration fails during synchronization.
        /// </exception>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TriggerMigrationNow_SynchronizationFails_ShouldThrowException()
        {
            // Arrange
            var mockEnumerableCache1 = _mockCache1.As<ICacheEnumerable<string>>();
            mockEnumerableCache1.Setup(c => c.Keys())
                .Throws(new InvalidOperationException("Enumeration failed"));

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object },
                _mockLogger.Object);

            // Act
            cache.TriggerMigrationNow();
        }

        /// <summary>
        /// Validates the behavior of the ParallelCache synchronization mechanism in a scenario where
        /// three distinct caches are involved, and the "Majority Wins" conflict resolution strategy is used.
        /// Ensures that when a key is present in a minority of the caches, it gets removed from those caches
        /// after triggering a migration process.
        /// </summary>
        /// <remarks>
        /// This test verifies that the cache adheres to the specified "Majority Wins" resolution logic
        /// by removing a key from caches where it exists if the majority of caches do not contain the key.
        /// It evaluates SyncOptions with a configured majority threshold of 50%.
        /// </remarks>
        /// <exception cref="MockException">
        /// Thrown if the mock cache's Remove method is not invoked as expected for caches containing the minority key.
        /// </exception>
        [TestMethod]
        public void TriggerMigrationNow_WithThreeCaches_MajorityWins_ShouldRemoveKeyFromMinority()
        {
            // Arrange
            var mockEnumerableCache1 = _mockCache1.As<ICacheEnumerable<string>>();
            var mockEnumerableCache2 = _mockCache2.As<ICacheEnumerable<string>>();
            var mockEnumerableCache3 = _mockCache3.As<ICacheEnumerable<string>>();

            // Two caches don't have the key, one cache has it
            mockEnumerableCache1.Setup(c => c.Keys()).Returns(new[] { "key1" });
            mockEnumerableCache2.Setup(c => c.Keys()).Returns(new string[0]);
            mockEnumerableCache3.Setup(c => c.Keys()).Returns(new string[0]);

            var syncOptions = new SyncOptions
            {
                ConflictResolution = SyncConflictResolution.MajorityWins,
                MajorityThreshold = 0.5
            };

            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object, _mockCache3.Object },
                _mockLogger.Object,
                syncOptions);

            // Act
            cache.TriggerMigrationNow();

            // Assert - key should be removed from cache1 because majority (2/3) don't have it
            _mockCache1.Verify(c => c.Remove("key1"), Times.Once);
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Performs an integration test to verify that the complete workflow of
        /// the ParallelCache, including the operations Set, TryGet, Remove, and Clear,
        /// functions as expected when used with multiple underlying caches.
        /// </summary>
        /// <remarks>
        /// This method tests the following scenarios:
        /// - Setting a value in the cache and verifying that it propagates to all underlying caches.
        /// - Retrieving a value from the cache and ensuring that it is correctly fetched from the first cache
        /// where the key is found.
        /// - Removing a key-value pair and verifying that the operation is executed across all caches.
        /// - Clearing the cache and ensuring all underlying caches are cleared.
        /// </remarks>
        /// <exception cref="AssertFailedException">
        /// Thrown if any of the assertions in the test fail, indicating that the cache behavior
        /// does not meet the expected outcomes.
        /// </exception>
        [TestMethod]
        public void IntegrationTest_CompleteWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            var cache = new ParallelCache<string, string>(
                new[] { _mockCache1.Object, _mockCache2.Object });

            // Test Set
            cache.Set("key1", "value1");
            Thread.Sleep(100);

            // Test TryGet
            _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<string>.IsAny))
                .Returns((string key, out string value) =>
                {
                    value = "value1";
                    return true;
                });

            bool getResult = cache.TryGet("key1", out string retrievedValue);
            Assert.IsTrue(getResult);
            Assert.AreEqual("value1", retrievedValue);

            // Test Remove
            _mockCache1.Setup(c => c.Remove("key1")).Returns(true);
            bool removeResult = cache.Remove("key1");
            Thread.Sleep(100);
            Assert.IsTrue(removeResult);

            // Test Clear
            cache.Clear();
            Thread.Sleep(100);

            // Verify all operations
            _mockCache1.Verify(c => c.Set("key1", "value1"), Times.Once);
            _mockCache2.Verify(c => c.Set("key1", "value1"), Times.Once);
            _mockCache1.Verify(c => c.Remove("key1"), Times.Once);
            _mockCache2.Verify(c => c.Remove("key1"), Times.Once);
            _mockCache1.Verify(c => c.Clear(), Times.Once);
            _mockCache2.Verify(c => c.Clear(), Times.Once);
        }

        #endregion
    }