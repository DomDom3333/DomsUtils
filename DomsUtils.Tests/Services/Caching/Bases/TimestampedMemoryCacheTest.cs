using System;
using System.Linq;
using DomsUtils.Services.Caching.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DomsUtils.Tests.Services.Caching.Bases;

[TestClass]
public class TimestampedMemoryCacheTest
{
    private TimestampedMemoryCache<string, string> _cache = null!;

    [TestInitialize]
    public void Setup()
    {
        _cache = new TimestampedMemoryCache<string, string>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _cache.Clear();
    }

    [TestMethod]
    public void Constructor_Default_Initializes()
    {
        var cache = new TimestampedMemoryCache<string, int>();
        Assert.IsNotNull(cache);
        Assert.IsTrue(cache.IsAvailable());
    }

    [TestMethod]
    public void SetAndTryGetWithTimestamp_Works()
    {
        DateTimeOffset ts = DateTimeOffset.UtcNow.AddMinutes(-1);
        _cache.SetWithTimestamp("a", "b", ts);
        bool ok = _cache.TryGetWithTimestamp("a", out string val, out DateTimeOffset rts);
        Assert.IsTrue(ok);
        Assert.AreEqual("b", val);
        Assert.AreEqual(ts, rts);
    }

    [TestMethod]
    public void Set_UsesCurrentTimestamp()
    {
        _cache.Set("key", "value");
        bool ok = _cache.TryGetWithTimestamp("key", out _, out DateTimeOffset ts);
        Assert.IsTrue(ok);
        Assert.IsTrue((DateTimeOffset.UtcNow - ts) < TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void Remove_RemovesItem()
    {
        _cache.Set("k", "v");
        bool removed = _cache.Remove("k");
        Assert.IsTrue(removed);
        Assert.IsFalse(_cache.TryGet("k", out _));
    }

    [TestMethod]
    public void Clear_RemovesAll()
    {
        _cache.Set("a", "1");
        _cache.Set("b", "2");
        _cache.Clear();
        Assert.AreEqual(0, _cache.Keys().Count());
    }

    [TestMethod]
    public void Keys_ReturnsAllKeys()
    {
        _cache.Set("a", "1");
        _cache.Set("b", "2");
        var keys = _cache.Keys().ToList();
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, keys);
    }
}
