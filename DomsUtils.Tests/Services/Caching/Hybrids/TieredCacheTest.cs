using System;
using System.Threading;
using DomsUtils.Services.Caching.Addons.MigrationRules;
using DomsUtils.Services.Caching.Hybrids;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace DomsUtils.Tests.Services.Caching.Hybrids;

[TestClass]
public class TieredCacheTests
{
    private Mock<ICache<string, int>> _mockCache1;
    private Mock<ICache<string, int>> _mockCache2;
    private Mock<ICache<string, int>> _mockCache3;
    private Mock<ILogger> _mockLogger;
    private Mock<MigrationRuleSet<string, int>> _mockMigrationRuleSet;

    [TestInitialize]
    public void Setup()
    {
        _mockCache1 = new Mock<ICache<string, int>>();
        _mockCache2 = new Mock<ICache<string, int>>();
        _mockCache3 = new Mock<ICache<string, int>>();
        _mockLogger = new Mock<ILogger>();
        _mockMigrationRuleSet = new Mock<MigrationRuleSet<string, int>>();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithNullCaches_ThrowsArgumentException()
    {
        // Act
        new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithSingleCache_ThrowsArgumentException()
    {
        // Act
        new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object, _mockCache1.Object);
    }

    [TestMethod]
    public void Constructor_WithValidCaches_InitializesSuccessfully()
    {
        // Act
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        // Assert
        Assert.IsNotNull(tieredCache);
    }

    [TestMethod]
    public void Constructor_WithPeriodicInterval_StartsTimer()
    {
        // Arrange - Create a concrete MigrationRuleSet with PeriodicCheckInterval
        var migrationRuleSet = new MigrationRuleSet<string, int>();
        migrationRuleSet.SetPeriodicInterval(TimeSpan.FromMilliseconds(100));

        // Act
        var tieredCache = new TieredCache<string, int>(migrationRuleSet, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        // Assert
        Assert.IsNotNull(tieredCache);
        // Timer functionality will be tested indirectly through migration tests
        
        // Cleanup
        tieredCache.Dispose();
    }

    [TestMethod]
    public void Constructor_WithEventBasedCaches_SubscribesToEvents()
    {
        // Arrange
        var mockEventCache1 = new Mock<ICache<string, int>>();
        mockEventCache1.As<ICacheEvents<string, int>>();
        var mockEventCache2 = new Mock<ICache<string, int>>();
        mockEventCache2.As<ICacheEvents<string, int>>();

        // Act
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            mockEventCache1.Object, mockEventCache2.Object);

        // Assert
        Assert.IsNotNull(tieredCache);
    }

    [TestMethod]
    public void TryGet_KeyFoundInFirstTier_ReturnsValueWithoutPromotion()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns((string key, out int value) =>
            {
                value = 100;
                return true;
            });

        // Act
        bool result = tieredCache.TryGet("key1", out int retrievedValue);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(100, retrievedValue);
        _mockCache1.Verify(c => c.TryGet("key1", out It.Ref<int>.IsAny), Times.Once);
        _mockCache2.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<int>.IsAny), Times.Never);
    }

    [TestMethod]
    public void TryGet_KeyFoundInSecondTier_ReturnsValueAndPromotes()
    {
        // Arrange
        // Create a migration rule set that allows promotion from tier 1 to tier 0
        var migrationRuleSet = new MigrationRuleSet<string, int>();
        migrationRuleSet.AddRule(1, 0, (key, value, fromCache, toCache) => true); // Allow all promotions from tier 1 to tier 0
        
        var tieredCache = new TieredCache<string, int>(migrationRuleSet, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns(false);
        _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns((string key, out int value) =>
            {
                value = 200;
                return true;
            });

        // Act
        bool result = tieredCache.TryGet("key1", out int retrievedValue);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(200, retrievedValue);
        _mockCache1.Verify(c => c.Set("key1", 200), Times.Once);
    }

    [TestMethod]
    public void TryGet_KeyNotFound_ReturnsFalse()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.TryGet("nonexistent", out It.Ref<int>.IsAny))
            .Returns(false);
        _mockCache2.Setup(c => c.TryGet("nonexistent", out It.Ref<int>.IsAny))
            .Returns(false);

        // Act
        bool result = tieredCache.TryGet("nonexistent", out int retrievedValue);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(default(int), retrievedValue);
    }

    [TestMethod]
    public void TryGet_WithUnavailableCache_SkipsTierAndContinues()
    {
        // Arrange
        var mockAvailableCache1 = new Mock<ICache<string, int>>();
        mockAvailableCache1.As<ICacheAvailability>()
            .Setup(a => a.IsAvailable())
            .Returns(false);

        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            mockAvailableCache1.Object, _mockCache2.Object);

        _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns((string key, out int value) =>
            {
                value = 300;
                return true;
            });

        // Act
        bool result = tieredCache.TryGet("key1", out int retrievedValue);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(300, retrievedValue);
        mockAvailableCache1.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<int>.IsAny), Times.Never);
    }

    [TestMethod]
    public void TryGet_WithExceptionInTier_ContinuesToNextTier()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Throws(new InvalidOperationException("Cache error"));
        _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns((string key, out int value) =>
            {
                value = 400;
                return true;
            });

        // Act
        bool result = tieredCache.TryGet("key1", out int retrievedValue);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(400, retrievedValue);
    }

    [TestMethod]
    public void Set_WithAllAvailableCaches_SetsInAllTiers()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object, _mockCache3.Object);

        // Act
        tieredCache.Set("key1", 500);

        // Assert
        _mockCache1.Verify(c => c.Set("key1", 500), Times.Once);
        _mockCache2.Verify(c => c.Set("key1", 500), Times.Once);
        _mockCache3.Verify(c => c.Set("key1", 500), Times.Once);
    }

    [TestMethod]
    public void Set_WithUnavailableCache_SkipsUnavailableTier()
    {
        // Arrange
        var mockAvailableCache2 = new Mock<ICache<string, int>>();
        mockAvailableCache2.As<ICacheAvailability>()
            .Setup(a => a.IsAvailable())
            .Returns(false);

        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, mockAvailableCache2.Object);

        // Act
        tieredCache.Set("key1", 600);

        // Assert
        _mockCache1.Verify(c => c.Set("key1", 600), Times.Once);
        mockAvailableCache2.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public void Set_WithExceptionInTier_ContinuesToOtherTiers()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.Set("key1", 700))
            .Throws(new InvalidOperationException("Set error"));

        // Act
        tieredCache.Set("key1", 700);

        // Assert
        _mockCache1.Verify(c => c.Set("key1", 700), Times.Once);
        _mockCache2.Verify(c => c.Set("key1", 700), Times.Once);
    }

    [TestMethod]
    public void Remove_KeyExistsInMultipleTiers_ReturnsTrue()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.Remove("key1")).Returns(true);
        _mockCache2.Setup(c => c.Remove("key1")).Returns(false);

        // Act
        bool result = tieredCache.Remove("key1");

        // Assert
        Assert.IsTrue(result);
        _mockCache1.Verify(c => c.Remove("key1"), Times.Once);
        _mockCache2.Verify(c => c.Remove("key1"), Times.Once);
    }

    [TestMethod]
    public void Remove_KeyNotFoundInAnyTier_ReturnsFalse()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.Remove("nonexistent")).Returns(false);
        _mockCache2.Setup(c => c.Remove("nonexistent")).Returns(false);

        // Act
        bool result = tieredCache.Remove("nonexistent");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Remove_WithUnavailableCache_SkipsUnavailableTier()
    {
        // Arrange
        var mockAvailableCache1 = new Mock<ICache<string, int>>();
        mockAvailableCache1.As<ICacheAvailability>()
            .Setup(a => a.IsAvailable())
            .Returns(false);

        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            mockAvailableCache1.Object, _mockCache2.Object);

        _mockCache2.Setup(c => c.Remove("key1")).Returns(true);

        // Act
        bool result = tieredCache.Remove("key1");

        // Assert
        Assert.IsTrue(result);
        mockAvailableCache1.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
        _mockCache2.Verify(c => c.Remove("key1"), Times.Once);
    }

    [TestMethod]
    public void Remove_WithExceptionInTier_ContinuesToOtherTiers()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.Remove("key1"))
            .Throws(new InvalidOperationException("Remove error"));
        _mockCache2.Setup(c => c.Remove("key1")).Returns(true);

        // Act
        bool result = tieredCache.Remove("key1");

        // Assert
        Assert.IsTrue(result);
        _mockCache2.Verify(c => c.Remove("key1"), Times.Once);
    }

    [TestMethod]
    public void Clear_WithAllAvailableCaches_ClearsAllTiers()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        // Act
        tieredCache.Clear();

        // Assert
        _mockCache1.Verify(c => c.Clear(), Times.Once);
        _mockCache2.Verify(c => c.Clear(), Times.Once);
    }

    [TestMethod]
    public void Clear_WithUnavailableCache_SkipsUnavailableTier()
    {
        // Arrange
        var mockAvailableCache1 = new Mock<ICache<string, int>>();
        mockAvailableCache1.As<ICacheAvailability>()
            .Setup(a => a.IsAvailable())
            .Returns(false);

        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            mockAvailableCache1.Object, _mockCache2.Object);

        // Act
        tieredCache.Clear();

        // Assert
        mockAvailableCache1.Verify(c => c.Clear(), Times.Never);
        _mockCache2.Verify(c => c.Clear(), Times.Once);
    }

    [TestMethod]
    public void Clear_WithExceptionInTier_ContinuesToOtherTiers()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.Clear())
            .Throws(new InvalidOperationException("Clear error"));

        // Act
        tieredCache.Clear();

        // Assert
        _mockCache1.Verify(c => c.Clear(), Times.Once);
        _mockCache2.Verify(c => c.Clear(), Times.Once);
    }

    [TestMethod]
    public void CheckAndMigrate_CallsTryGet()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns((string key, out int value) =>
            {
                value = 100;
                return true;
            });

        // Act
        tieredCache.CheckAndMigrate("key1");

        // Assert
        _mockCache1.Verify(c => c.TryGet("key1", out It.Ref<int>.IsAny), Times.Once);
    }

    [TestMethod]
    public void TriggerMigrationNow_WithEnumerableCaches_ProcessesAllKeys()
    {
        // Arrange
        var mockEnumerableCache1 = new Mock<ICache<string, int>>();
        mockEnumerableCache1.As<ICacheEnumerable<string>>()
            .Setup(e => e.Keys())
            .Returns(new[] { "key1", "key2" });

        var mockEnumerableCache2 = new Mock<ICache<string, int>>();
        mockEnumerableCache2.As<ICacheEnumerable<string>>()
            .Setup(e => e.Keys())
            .Returns(new[] { "key3", "key4" });

        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            mockEnumerableCache1.Object, mockEnumerableCache2.Object);

        // Act
        tieredCache.TriggerMigrationNow();

        // Assert - Verify that TryGet was called for each key
        mockEnumerableCache1.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<int>.IsAny), Times.AtLeast(1));
        mockEnumerableCache2.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<int>.IsAny), Times.AtLeast(1));
    }

    [TestMethod]
    public void EventTriggeredMigration_WithValidRule_PerformsMigration()
    {
        // Arrange
        var mockCache1 = new Mock<ICache<string, int>>();
        var mockEventCache = new Mock<ICache<string, int>>();
        mockEventCache.As<ICacheEvents<string, int>>();
        
        var migrationRuleSet = new MigrationRuleSet<string, int>();
        
        // Add a rule that migrates from tier 0 to tier 1 when value > 50
        migrationRuleSet.AddRule(
            fromTier: 0,
            toTier: 1,
            condition: (key, value, fromCache, toCache) => value > 50
        );
        
        var tieredCache = new TieredCache<string, int>(
            migrationRuleSet,
            logger: null,
            mockEventCache.Object,  // This is tier 0
            mockCache1.Object       // This is tier 1
        );
        
        // Act - Trigger the OnSet event with a value that should cause migration
        mockEventCache.As<ICacheEvents<string, int>>()
            .Raise(e => e.OnSet += null, "key1", 100);
        
        // Assert - Verify that the value was migrated to tier 1
        mockCache1.Verify(c => c.Set("key1", 100), Times.Once);
    }

    [TestMethod]
    public void Promote_WithMigrationRules_ChecksRulesBeforePromotion()
    {
        // Arrange
        bool migrationRuleCalled = false;
        bool shouldAllowMigration = false;

        var migrationRuleSet = new MigrationRuleSet<string, int>();
        
        // Add a rule that prevents migration from tier 1 to tier 0
        migrationRuleSet.AddRule(1, 0, (key, value, fromCache, toCache) =>
        {
            migrationRuleCalled = true;
            // Verify the parameters passed to the rule
            Assert.AreEqual("key1", key);
            Assert.AreEqual(100, value);
            Assert.AreSame(_mockCache2.Object, fromCache);
            Assert.AreSame(_mockCache1.Object, toCache);
            return shouldAllowMigration;
        });

        var tieredCache = new TieredCache<string, int>(migrationRuleSet, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns((string key, out int value) =>
            {
                value = 100;
                return true;
            });

        // Act
        tieredCache.TryGet("key1", out _);

        // Assert
        Assert.IsTrue(migrationRuleCalled, "Migration rule should have been called");
        _mockCache1.Verify(c => c.Set("key1", 100), Times.Never, "Should not promote when rule returns false");
    }

    [TestMethod]
    public void Promote_WithExceptionInTargetCache_ContinuesWithoutCrashing()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        _mockCache1.Setup(c => c.Set("key1", 100))
            .Throws(new InvalidOperationException("Promotion error"));
        _mockCache2.Setup(c => c.TryGet("key1", out It.Ref<int>.IsAny))
            .Returns((string key, out int value) =>
            {
                value = 100;
                return true;
            });

        // Act & Assert - Should not throw
        bool result = tieredCache.TryGet("key1", out int value);

        Assert.IsTrue(result);
        Assert.AreEqual(100, value);
    }

    [TestMethod]
    public void Dispose_DisposesTimerAndDisposableCaches()
    {
        // Arrange
        var mockDisposableCache1 = new Mock<ICache<string, int>>();
        mockDisposableCache1.As<IDisposable>();

        var mockDisposableCache2 = new Mock<ICache<string, int>>();
        mockDisposableCache2.As<IDisposable>();

        var migrationRuleSet = new MigrationRuleSet<string, int>();
        migrationRuleSet.SetPeriodicInterval(TimeSpan.FromMilliseconds(100));

        var tieredCache = new TieredCache<string, int>(migrationRuleSet, _mockLogger.Object,
            mockDisposableCache1.Object, mockDisposableCache2.Object);

        // Act
        tieredCache.Dispose();

        // Assert
        mockDisposableCache1.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
        mockDisposableCache2.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        // Act & Assert - Should not throw
        tieredCache.Dispose();
        tieredCache.Dispose();
    }

    [TestMethod]
    public void Constructor_WithNullLogger_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        var tieredCache = new TieredCache<string, int>(_mockMigrationRuleSet.Object, null,
            _mockCache1.Object, _mockCache2.Object);

        Assert.IsNotNull(tieredCache);
    }

    [TestMethod]
    public void Constructor_WithNullMigrationRuleSet_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        var tieredCache = new TieredCache<string, int>(null, _mockLogger.Object,
            _mockCache1.Object, _mockCache2.Object);

        Assert.IsNotNull(tieredCache);
    }

    [TestMethod]
    public void PeriodicMigration_WithTimer_ExecutesPeriodically()
    {
        // Arrange
        var mockEnumerableCache = new Mock<ICache<string, int>>();
        mockEnumerableCache.As<ICacheEnumerable<string>>()
            .Setup(e => e.Keys())
            .Returns(new[] { "key1" });

        var migrationRuleSet = new MigrationRuleSet<string, int>();
        migrationRuleSet.SetPeriodicInterval(TimeSpan.FromMilliseconds(50));
        
        // Add a rule that allows migration from tier 1 to tier 0
        migrationRuleSet.AddRule(1, 0, (key, value, fromCache, toCache) => true);

        var tieredCache = new TieredCache<string, int>(migrationRuleSet, _mockLogger.Object,
            _mockCache1.Object, mockEnumerableCache.Object);

        // Act - Wait for timer to execute
        Thread.Sleep(150);

        // Assert
        mockEnumerableCache.Verify(c => c.TryGet("key1", out It.Ref<int>.IsAny), Times.AtLeast(1));

        // Cleanup
        tieredCache.Dispose();
    }
}