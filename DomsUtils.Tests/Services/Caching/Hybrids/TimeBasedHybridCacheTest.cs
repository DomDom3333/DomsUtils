using System;
using System.Linq;
using DomsUtils.Services.Caching.Bases;
using DomsUtils.Services.Caching.Hybrids;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DomsUtils.Tests.Services.Caching.Hybrids;

[TestClass]
public class TimeBasedHybridCacheTest
{
    private TimestampedMemoryCache<string, int> _memory = null!;
    private Mock<ICache<string, int>> _persistent = null!;
    private TimeBasedHybridCache<string, int> _hybrid = null!;

    [TestInitialize]
    public void Setup()
    {
        _memory = new TimestampedMemoryCache<string, int>();
        _persistent = new Mock<ICache<string, int>>();
        _hybrid = new TimeBasedHybridCache<string, int>(_memory, _persistent.Object,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(25), Mock.Of<ILogger>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _hybrid.Dispose();
    }

    [TestMethod]
    public void TryGet_HitsMemory_ReturnsValue()
    {
        _hybrid.Set("a", 1);
        bool ok = _hybrid.TryGet("a", out int val);
        Assert.IsTrue(ok);
        Assert.AreEqual(1, val);
        _persistent.Verify(p => p.TryGet(It.IsAny<string>(), out It.Ref<int>.IsAny), Times.Never);
    }

    [TestMethod]
    public void TryGet_FetchesFromPersistent_WhenMissingInMemory()
    {
        _persistent.Setup(p => p.TryGet("b", out It.Ref<int>.IsAny))
            .Returns((string k, out int v) => { v = 2; return true; });
        bool ok = _hybrid.TryGet("b", out int val);
        Assert.IsTrue(ok);
        Assert.AreEqual(2, val);
        Assert.IsTrue(_memory.TryGet("b", out _));
    }

    [TestMethod]
    public void Migration_MovesOldEntriesToPersistent()
    {
        _hybrid.Set("c", 3);
        // advance time by waiting > demotionAge
        System.Threading.Thread.Sleep(60);
        _hybrid.TriggerMigrationNow();
        Assert.IsFalse(_memory.TryGet("c", out _));
        _persistent.Verify(p => p.Set("c", 3), Times.AtLeastOnce);
    }

    [TestMethod]
    public void Remove_RemovesFromBothCaches()
    {
        _hybrid.Set("d", 4);
        _hybrid.Remove("d");
        Assert.IsFalse(_memory.TryGet("d", out _));
        _persistent.Verify(p => p.Remove("d"), Times.Once);
    }
}
