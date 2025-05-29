using System;
using System.Collections.Generic;
using DomsUtils.Classes.BiMap.Extensions;
using DomsUtils.Classes.BiMap.Base;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DomsUtils.Tests.Classes.BiMap.Extensions;

[TestClass]
[TestSubject(typeof(BiMapExtensions))]
public class BiMapExtensionsTest
{
    [TestMethod]
    public void ToBiMap_ValidDictionary_ReturnsBiMap()
    {
        // Arrange
        var dictionary = new Dictionary<int, string>
        {
            { 1, "One" },
            { 2, "Two" },
            { 3, "Three" }
        };

        // Act
        var biMap = dictionary.ToBiMap();

        // Assert
        Assert.AreEqual(3, biMap.Count);
        Assert.AreEqual("One", biMap[1]);
        Assert.AreEqual(1, biMap["One"]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ToBiMap_NullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        Dictionary<int, string>? dictionary = null;

        // Act
        dictionary!.ToBiMap();

        // Assert - Expects exception
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ToBiMap_DictionaryWithDuplicateValues_ThrowsArgumentException()
    {
        // Arrange
        var dictionary = new Dictionary<int, string>
        {
            { 1, "Duplicate" },
            { 2, "Duplicate" }
        };

        // Act
        dictionary.ToBiMap();

        // Assert - Expects exception
    }

    [TestMethod]
    public void ToBiMapSafe_ValidDictionaryWithConflictResolver_ReplacesConflictingValues()
    {
        // Arrange
        var dictionary = new Dictionary<int, string>
        {
            { 1, "Value1" },
            { 2, "Value2" },
            { 3, "Value1" }
        };

        // Define conflict resolver: Always resolve conflicts by replacing
        Func<int, string, bool> conflictResolver = (key, value) => value == "Value1";

        // Act
        var biMap = dictionary.ToBiMapSafe(conflictResolver);

        // Assert
        Assert.AreEqual(2, biMap.Count);
        Assert.IsTrue(biMap.ContainsKey(3));
        Assert.AreEqual("Value1", biMap[3]);
    }

    [TestMethod]
    public void ToBiMapSafe_NullConflictResolver_IgnoresConflicts()
    {
        // Arrange
        var dictionary = new Dictionary<int, string>
        {
            { 1, "Value1" },
            { 2, "Value1" }
        };

        // Act
        var biMap = dictionary.ToBiMapSafe();

        // Assert
        Assert.AreEqual(1, biMap.Count);
    }

    [TestMethod]
    public void ToBiMap_EnumerableWithKeyAndValueSelectors_ReturnsBiMap()
    {
        // Arrange
        var list = new[]
        {
            new KeyValuePair<int, string>(1, "Value1"),
            new KeyValuePair<int, string>(2, "Value2")
        };

        // Act
        var biMap = list.ToBiMap(item => item.Key, item => item.Value);

        // Assert
        Assert.AreEqual(2, biMap.Count);
        Assert.AreEqual("Value1", biMap[1]);
        Assert.AreEqual(1, biMap["Value1"]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ToBiMap_Enumerable_NullKeySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var list = new[]
        {
            new KeyValuePair<int, string>(1, "Value1"),
        };

        // Act
        list.ToBiMap<KeyValuePair<int, string>, string, string>(null!, item => item.Value);

        // Assert - Expects exception
    }

    [TestMethod]
    public void ToBiMapSafe_EnumerableWithConflictResolution_ReturnsResolvedBiMap()
    {
        // Arrange
        var list = new[]
        {
            new KeyValuePair<int, string>(1, "Value1"),
            new KeyValuePair<int, string>(2, "Value1")
        };

        // Define conflict resolver
        Func<int, string, bool> conflictResolver = (key, value) => true;

        // Act
        var biMap = list.ToBiMapSafe(item => item.Key, item => item.Value, conflictResolver);

        // Assert
        Assert.AreEqual(1, biMap.Count);
        Assert.AreEqual("Value1", biMap[2]);
    }
}