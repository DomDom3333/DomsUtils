using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomsUtils.DataStructures.BiMap.Children.Observable;
using JetBrains.Annotations;

namespace DomsUtils.Tests.Classes.BiMap.Children.Observable;

[TestClass]
[TestSubject(typeof(ObservableBiMap<,>))]
public class ObservableBiMapTest
{
    private ObservableBiMap<int, string> _biMap;

    [TestInitialize]
    public void Setup()
    {
        _biMap = new ObservableBiMap<int, string>();
    }

    [TestMethod]
    public void Add_ShouldAddKeyValuePair_AndTriggerNotifications()
    {
        bool collectionChangedRaised = false;
        bool propertyChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;
        _biMap.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_biMap.Count))
                propertyChangedRaised = true;
        };

        _biMap.Add(1, "test");

        Assert.IsTrue(collectionChangedRaised);
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(1, _biMap.Count);
        Assert.AreEqual("test", _biMap[1]);
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void Indexer_ShouldThrowWhenKeyNotFound()
    {
        _ = _biMap[1];
    }

    [TestMethod]
    public void RemoveKey_ShouldRemoveKeyValuePair_AndTriggerNotifications()
    {
        _biMap.Add(1, "test");

        bool collectionChangedRaised = false;
        bool propertyChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;
        _biMap.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_biMap.Count))
                propertyChangedRaised = true;
        };

        bool result = _biMap.RemoveByKey(1);

        Assert.IsTrue(result);
        Assert.IsTrue(collectionChangedRaised);
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(0, _biMap.Count);
    }

    [TestMethod]
    public void Contains_ShouldReturnCorrectValue()
    {
        _biMap.Add(1, "test");

        Assert.IsTrue(_biMap.Contains(new KeyValuePair<int, string>(1, "test")));
        Assert.IsFalse(_biMap.Contains(new KeyValuePair<int, string>(2, "test2")));
    }

    [TestMethod]
    public void Replace_ShouldReplaceValueForKey_AndTriggerNotifications()
    {
        _biMap.Add(1, "test");

        bool collectionChangedRaised = false;
        bool propertyChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;
        _biMap.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_biMap.Values))
                propertyChangedRaised = true;
        };

        bool result = _biMap.Replace(1, "updated");

        Assert.IsTrue(result);
        Assert.IsTrue(collectionChangedRaised);
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(1, _biMap.Count);
        Assert.AreEqual("updated", _biMap[1]);
    }

    [TestMethod]
    public void Clear_ShouldRemoveAllEntries_AndTriggerNotifications()
    {
        _biMap.Add(1, "test");
        _biMap.Add(2, "test2");

        bool collectionChangedRaised = false;
        bool propertyChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;
        _biMap.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_biMap.Count))
                propertyChangedRaised = true;
        };

        _biMap.Clear();

        Assert.IsTrue(collectionChangedRaised);
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(0, _biMap.Count);
    }

    [TestMethod]
    public void TryAdd_ShouldAddWhenKeyAndValueDoNotExist()
    {
        bool result = _biMap.TryAdd(1, "test");

        Assert.IsTrue(result);
        Assert.AreEqual(1, _biMap.Count);
        Assert.AreEqual("test", _biMap[1]);
    }

    [TestMethod]
    public async Task TryAddAsync_ShouldAddWhenKeyAndValueDoNotExist()
    {
        bool result = await _biMap.TryAddAsync(1, "test");

        Assert.IsTrue(result);
        Assert.AreEqual(1, _biMap.Count);
        Assert.AreEqual("test", _biMap[1]);
    }

    [TestMethod]
    public void CopyTo_ShouldCopyElementsToArray()
    {
        _biMap.Add(1, "test");
        _biMap.Add(2, "test2");

        KeyValuePair<int, string>[] array = new KeyValuePair<int, string>[2];
        _biMap.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(array, _biMap.ToDictionary().ToArray());
    }

    [TestMethod]
    public void RemoveByValue_ShouldRemoveItemWithValue_AndTriggerNotifications()
    {
        _biMap.Add(1, "test");

        bool collectionChangedRaised = false;
        bool propertyChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;
        _biMap.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_biMap.Count))
                propertyChangedRaised = true;
        };

        bool result = _biMap.RemoveByValue("test");

        Assert.IsTrue(result);
        Assert.IsTrue(collectionChangedRaised);
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(0, _biMap.Count);
    }

    [TestMethod]
    public void ContainsKey_ShouldReturnTrueIfKeyExists()
    {
        _biMap.Add(1, "test");

        Assert.IsTrue(_biMap.ContainsKey(1));
        Assert.IsFalse(_biMap.ContainsKey(2));
    }

    [TestMethod]
    public async Task RemoveByKeyAsync_ShouldRemoveKey_AndTriggerEvents()
    {
        _biMap.Add(1, "test");

        bool collectionChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;

        bool result = await _biMap.RemoveByKeyAsync(1, CancellationToken.None);

        Assert.IsTrue(result);
        Assert.IsTrue(collectionChangedRaised);
        Assert.AreEqual(0, _biMap.Count);
    }

    [TestMethod]
    public void Keys_ShouldReturnAllKeys()
    {
        _biMap.Add(1, "test");
        _biMap.Add(2, "test2");

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, _biMap.Keys.ToArray());
    }

    [TestMethod]
    public void Values_ShouldReturnAllValues()
    {
        _biMap.Add(1, "test");
        _biMap.Add(2, "test2");

        CollectionAssert.AreEquivalent(new[] { "test", "test2" }, _biMap.Values.ToArray());
    }

    [TestMethod]
    public void ReplaceByValue_ShouldReplaceKeyForValue_AndTriggerNotifications()
    {
        _biMap.Add(1, "test");

        bool collectionChangedRaised = false;
        bool propertyChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;
        _biMap.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_biMap.Keys))
                propertyChangedRaised = true;
        };

        bool result = _biMap.ReplaceByValue("test", 2);

        Assert.IsTrue(result);
        Assert.IsTrue(collectionChangedRaised);
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(1, _biMap.Count);
        Assert.AreEqual(2, _biMap["test"]);
    }

    [TestMethod]
    public void ReplaceByValue_ShouldReturnFalse_WhenValueNotFound()
    {
        bool result = _biMap.ReplaceByValue("nonexistent", 1);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ReplaceByValue_ShouldReturnFalse_WhenNewKeyAlreadyExists()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        bool result = _biMap.ReplaceByValue("test1", 2);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ContainsValue_ShouldReturnTrueIfValueExists()
    {
        _biMap.Add(1, "test");

        Assert.IsTrue(_biMap.ContainsValue("test"));
        Assert.IsFalse(_biMap.ContainsValue("nonexistent"));
    }

    [TestMethod]
    public async Task RemoveByValueAsync_ShouldRemoveValue_AndTriggerEvents()
    {
        _biMap.Add(1, "test");

        bool collectionChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;

        bool result = await _biMap.RemoveByValueAsync("test", CancellationToken.None);

        Assert.IsTrue(result);
        Assert.IsTrue(collectionChangedRaised);
        Assert.AreEqual(0, _biMap.Count);
    }

    [TestMethod]
    public void TryGetKeysToSpan_ShouldCopyKeysToSpan()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        Span<int> keys = new int[2];
        bool result = _biMap.TryGetKeysToSpan(keys);

        Assert.IsTrue(result);
        Assert.IsTrue(keys.Contains(1));
        Assert.IsTrue(keys.Contains(2));
    }

    [TestMethod]
    public void TryGetKeysToSpan_ShouldReturnFalse_WhenSpanTooSmall()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        Span<int> keys = new int[1];
        bool result = _biMap.TryGetKeysToSpan(keys);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryGetValuesToSpan_ShouldCopyValuesToSpan()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        Span<string> values = new string[2];
        bool result = _biMap.TryGetValuesToSpan(values);

        Assert.IsTrue(result);
        Assert.IsTrue(values.Contains("test1"));
        Assert.IsTrue(values.Contains("test2"));
    }

    [TestMethod]
    public void TryGetValuesToSpan_ShouldReturnFalse_WhenSpanTooSmall()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        Span<string> values = new string[1];
        bool result = _biMap.TryGetValuesToSpan(values);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryGetByKeyToSpan_ShouldCopyValueToSpan()
    {
        _biMap.Add(1, "test");

        Span<string> value = new string[1];
        bool result = _biMap.TryGetByKeyToSpan(1, value);

        Assert.IsTrue(result);
        Assert.AreEqual("test", value[0]);
    }

    [TestMethod]
    public void TryGetByKeyToSpan_ShouldReturnFalse_WhenKeyNotFound()
    {
        Span<string> value = new string[1];
        bool result = _biMap.TryGetByKeyToSpan(1, value);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryGetByKeyToSpan_ShouldReturnFalse_WhenSpanTooSmall()
    {
        _biMap.Add(1, "test");

        Span<string> value = new string[0];
        bool result = _biMap.TryGetByKeyToSpan(1, value);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryGetMultipleByKeys_ShouldCopyValuesToSpan()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        ReadOnlySpan<int> keys = new[] { 1, 2 };
        Span<string> values = new string[2];

        bool result = _biMap.TryGetMultipleByKeys(keys, values);

        Assert.IsTrue(result);
        Assert.AreEqual("test1", values[0]);
        Assert.AreEqual("test2", values[1]);
    }

    [TestMethod]
    public void TryGetMultipleByKeys_ShouldReturnFalse_WhenKeyNotFound()
    {
        _biMap.Add(1, "test1");

        ReadOnlySpan<int> keys = new[] { 1, 2 };
        Span<string> values = new string[2];

        bool result = _biMap.TryGetMultipleByKeys(keys, values);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryGetMultipleByKeys_ShouldReturnFalse_WhenDestinationTooSmall()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        ReadOnlySpan<int> keys = new[] { 1, 2 };
        Span<string> values = new string[1];

        bool result = _biMap.TryGetMultipleByKeys(keys, values);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CopyToSpan_ShouldCopyElementsToSpan()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        Span<KeyValuePair<int, string>> array = new KeyValuePair<int, string>[2];
        _biMap.CopyTo(array);

        Assert.AreEqual(2, array.Length);
        bool contains = false;
        KeyValuePair<int, string> pair = new KeyValuePair<int, string>(1, "test1");
        foreach (var valuePair in array.ToArray())
        {
            if (Equals(valuePair, pair))
            {
                contains = true;
                break;
            }
        }

        Assert.IsTrue(contains);
        bool contains1 = false;
        KeyValuePair<int, string> keyValuePair = new KeyValuePair<int, string>(2, "test2");
        foreach (var valuePair in array.ToArray())
        {
            if (Equals(valuePair, keyValuePair))
            {
                contains1 = true;
                break;
            }
        }

        Assert.IsTrue(contains1);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CopyToSpan_ShouldThrowWhenDestinationTooSmall()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        Span<KeyValuePair<int, string>> array = new KeyValuePair<int, string>[1];
        _biMap.CopyTo(array);
    }

    [TestMethod]
    public async Task TryGetByKeyAsync_ShouldReturnValueForExistingKey()
    {
        _biMap.Add(1, "test");

        (bool exists, string? value) = await _biMap.TryGetByKeyAsync(1);

        Assert.IsTrue(exists);
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public async Task TryGetByKeyAsync_ShouldReturnFalseForNonExistingKey()
    {
        (bool exists, _) = await _biMap.TryGetByKeyAsync(1);

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public void Replace_ShouldReturnFalse_WhenKeyNotFound()
    {
        bool result = _biMap.Replace(1, "test");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Replace_ShouldReturnFalse_WhenNewValueAlreadyExists()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        bool result = _biMap.Replace(1, "test2");
        Assert.IsFalse(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CopyToArray_ShouldThrowWhenArrayIsNull()
    {
        _biMap.CopyTo(null!, 0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void CopyToArray_ShouldThrowWhenIndexIsNegative()
    {
        _biMap.CopyTo(new KeyValuePair<int, string>[1], -1);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CopyToArray_ShouldThrowWhenArrayTooSmall()
    {
        _biMap.Add(1, "test1");
        _biMap.Add(2, "test2");

        _biMap.CopyTo(new KeyValuePair<int, string>[1], 0);
    }

    [TestMethod]
    public void AddRange_ShouldAddMultipleItems_AndTriggerNotifications()
    {
        bool collectionChangedRaised = false;
        bool propertyChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;
        _biMap.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_biMap.Count))
                propertyChangedRaised = true;
        };

        var items = new Dictionary<int, string>
        {
            { 1, "test1" },
            { 2, "test2" }
        };

        _biMap.AddRange(items);

        Assert.IsTrue(collectionChangedRaised);
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(2, _biMap.Count);
        Assert.AreEqual("test1", _biMap[1]);
        Assert.AreEqual("test2", _biMap[2]);
    }

    [TestMethod]
    public void AddRange_ShouldDoNothing_WhenCollectionIsEmpty()
    {
        bool collectionChangedRaised = false;

        _biMap.CollectionChanged += (_, _) => collectionChangedRaised = true;

        _biMap.AddRange(new Dictionary<int, string>());

        Assert.IsFalse(collectionChangedRaised);
        Assert.AreEqual(0, _biMap.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void IndexerByValue_ShouldThrowWhenValueNotFound()
    {
        _ = _biMap["nonexistent"];
    }


}