using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using DomsUtils.Classes.BiMap.Base;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace DomsUtils.Tests.Classes.BiMap.Base;

[TestClass]
[TestSubject(typeof(BiMap<,>))]
public class BiMapTest
{
    [TestMethod]
    public void Constructor_Default_ShouldCreateEmptyBiMap()
    {
        BiMap<int, string> biMap = [];
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void Constructor_WithCapacity_ShouldCreateEmptyBiMap_WithCapacity()
    {
        BiMap<int, string> biMap = new(10);
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void Constructor_WithComparer_ShouldCreateBiMap()
    {
        BiMap<int, string> biMap = new(EqualityComparer<int>.Default, EqualityComparer<string>.Default, 10);
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void Constructor_WithItems_ShouldPopulateBiMap()
    {
        BiMap<int, string> biMap = new([new KeyValuePair<int, string>(1, "test")]);
        Assert.AreEqual(1, biMap.Count);
        Assert.AreEqual("test", biMap[1]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_WithNullItems_ShouldThrowArgumentNullException()
    {
        _ = new BiMap<int, string>(null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithDuplicateKeys_ShouldThrowArgumentException()
    {
        _ = new BiMap<int, string>([
            new KeyValuePair<int, string>(1, "test"),
            new KeyValuePair<int, string>(1, "duplicate")
        ]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithDuplicateValues_ShouldThrowArgumentException()
    {
        _ = new BiMap<int, string>([
            new KeyValuePair<int, string>(1, "test"),
            new KeyValuePair<int, string>(2, "test")
        ]);
    }

    [TestMethod]
    public void Add_ShouldAddKeyValuePair()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");
        Assert.AreEqual(1, biMap.Count);
    }

    [TestMethod]
    public void TryAdd_ShouldAddKeyValuePair_IfNotExists()
    {
        BiMap<int, string> biMap = [];
        bool result = biMap.TryAdd(1, "test");
        Assert.IsTrue(result);
        Assert.AreEqual(1, biMap.Count);
    }

    [TestMethod]
    public void TryAdd_ShouldNotAddKeyValuePair_IfDuplicateKeyOrValueExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "value1");

        bool resultKey = biMap.TryAdd(1, "value2");
        bool resultValue = biMap.TryAdd(2, "value1");

        Assert.IsFalse(resultKey);
        Assert.IsFalse(resultValue);
        Assert.AreEqual(1, biMap.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Add_ShouldThrowArgumentException_WhenDuplicateKeyOrValueAdded()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");
        biMap.Add(1, "duplicate");
    }

    [TestMethod]
    public void RemoveByKey_ShouldRemoveKeyValuePair()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        bool removed = biMap.RemoveByKey(1);
        Assert.IsTrue(removed);
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void RemoveByKey_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        bool removed = biMap.RemoveByKey(42);
        Assert.IsFalse(removed);
    }

    [TestMethod]
    public void RemoveByValue_ShouldRemoveKeyValuePair()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        bool removed = biMap.RemoveByValue("test");
        Assert.IsTrue(removed);
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void RemoveByValue_ShouldReturnFalse_WhenValueDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        bool removed = biMap.RemoveByValue("not-found");
        Assert.IsFalse(removed);
    }

    [TestMethod]
    public void TryGetByKey_ShouldReturnTrueAndValue_WhenKeyExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        bool result = biMap.TryGetByKey(1, out string value);
        Assert.IsTrue(result);
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void TryGetByKey_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        bool result = biMap.TryGetByKey(1, out string value);
        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    [TestMethod]
    public void TryGetByValue_ShouldReturnTrueAndKey_WhenValueExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        bool result = biMap.TryGetByValue("test", out int key);
        Assert.IsTrue(result);
        Assert.AreEqual(1, key);
    }

    [TestMethod]
    public void TryGetByValue_ShouldReturnFalse_WhenValueDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        bool result = biMap.TryGetByValue("test", out int key);
        Assert.IsFalse(result);
        Assert.AreEqual(default, key);
    }

    [TestMethod]
    public void ContainsKey_ShouldReturnTrue_IfKeyExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        Assert.IsTrue(biMap.ContainsKey(1));
    }

    [TestMethod]
    public void ContainsKey_ShouldReturnFalse_IfKeyDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        Assert.IsFalse(biMap.ContainsKey(1));
    }

    [TestMethod]
    public void ContainsValue_ShouldReturnTrue_IfValueExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        Assert.IsTrue(biMap.ContainsValue("test"));
    }

    [TestMethod]
    public void ContainsValue_ShouldReturnFalse_IfValueDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        Assert.IsFalse(biMap.ContainsValue("test"));
    }

    [TestMethod]
    public void Clear_ShouldRemoveAllItems()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        biMap.Clear();
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void ToDictionary_ShouldReturnDictionaryRepresentation()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");

        Dictionary<int, string> dict = biMap.ToDictionary();
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual("test", dict[1]);
    }

    [TestMethod]
    public void Keys_ShouldReturnAllKeys()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");
        biMap.Add(2, "test2");

        CollectionAssert.AreEqual(new[] { 1, 2 }, biMap.Keys.ToArray());
    }

    [TestMethod]
    public void Values_ShouldReturnAllValues()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "test");
        biMap.Add(2, "test2");

        CollectionAssert.AreEqual(new[] { "test", "test2" }, biMap.Values.ToArray());
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void Indexer_ByKey_ShouldThrow_WhenKeyDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        _ = biMap[42]; // Should throw
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void Indexer_ByValue_ShouldThrow_WhenValueDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        _ = biMap["not-found"]; // Should throw
    }

    [TestMethod]
    public void IsReadOnly_ShouldBeFalse()
    {
        BiMap<int, string> biMap = [];
        Assert.IsFalse(biMap.IsReadOnly);
    }

    [TestMethod]
    public void Enumerator_ShouldEnumerateAllItems()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ];
        BiMap<int, string> biMap = new(items);

        CollectionAssert.AreEquivalent(items, biMap.ToList());
    }

    [TestMethod]
    public void CopyTo_ShouldCopyAllItems()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ];
        BiMap<int, string> biMap = new(items);
        KeyValuePair<int, string>[] array = new KeyValuePair<int, string>[2];
        biMap.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(items, array);
    }

    [TestMethod]
    public void AddRange_ShouldAddAllItems()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ];
        BiMap<int, string> biMap = [];
        biMap.AddRange(items);

        Assert.AreEqual(2, biMap.Count);
        Assert.AreEqual("one", biMap[1]);
        Assert.AreEqual("two", biMap[2]);
    }

    [TestMethod]
    public void CustomKeyComparer_ShouldAffectKeyEquality()
    {
        // Using case-insensitive string comparer
        BiMap<string, int> biMap = new(StringComparer.OrdinalIgnoreCase, EqualityComparer<int>.Default) { { "key", 1 } };

        Assert.IsTrue(biMap.ContainsKey("KEY"));
        Assert.AreEqual(1, biMap["KEY"]);
    }

    [TestMethod]
    public void CustomValueComparer_ShouldAffectValueEquality()
    {
        // Using case-insensitive string comparer for value
        BiMap<int, string> biMap = new(EqualityComparer<int>.Default, StringComparer.OrdinalIgnoreCase) { { 1, "value" } };

        Assert.IsTrue(biMap.ContainsValue("VALUE"));
        Assert.AreEqual(1, biMap["VALUE"]);
    }

    [TestMethod]
    public async Task TryAddAsync_ShouldAddKeyValuePair_IfNotExists()
    {
        BiMap<int, string> biMap = [];
        bool result = await biMap.TryAddAsync(3, "three");
        Assert.IsTrue(result);
        Assert.AreEqual(1, biMap.Count);
    }

    [TestMethod]
    public async Task RemoveByKeyAsync_ShouldRemoveKey_WhenExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(4, "four");

        bool result = await biMap.RemoveByKeyAsync(4);
        Assert.IsTrue(result);
        Assert.IsFalse(biMap.ContainsKey(4));
    }

    [TestMethod]
    public async Task TryGetByKeyAsync_ShouldReturnValue_WhenKeyExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(5, "five");

        (bool exists, string value) = await biMap.TryGetByKeyAsync(5);
        Assert.IsTrue(exists);
        Assert.AreEqual("five", value);
    }
    
        [TestMethod]
    public void ReverseIndexer_ByValue_ShouldReturnKey()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(7, "seven");
        Assert.AreEqual(7, biMap["seven"]);
    }

    [TestMethod]
    public void ContainsPair_ShouldReturnTrue_IfPairExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "one");
        Assert.IsTrue(biMap.Contains(new KeyValuePair<int, string>(1, "one")));
    }

    [TestMethod]
    public void ContainsPair_ShouldReturnFalse_IfPairDoesNotExist()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "one");
        // wrong value
        Assert.IsFalse(biMap.Contains(new KeyValuePair<int, string>(1, "uno")));
        // wrong key
        Assert.IsFalse(biMap.Contains(new KeyValuePair<int, string>(2, "one")));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void AddRange_WithNull_ShouldThrowArgumentNullException()
    {
        BiMap<int, string> biMap = [];
        biMap.AddRange(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void AddRange_WithDuplicateKey_ShouldThrowArgumentException()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(1, "uno")
        ];
        BiMap<int, string> biMap = [];
        biMap.AddRange(items);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void AddRange_WithDuplicateValue_ShouldThrowArgumentException()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "one")
        ];
        BiMap<int, string> biMap = [];
        biMap.AddRange(items);
    }

    [TestMethod]
    public void CopyToSpan_ShouldCopyAllItems()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(10, "ten"),
            new KeyValuePair<int, string>(20, "twenty")
        ];
        BiMap<int, string> biMap = new(items);
        KeyValuePair<int, string>[] destination = new KeyValuePair<int, string>[2];
        biMap.CopyTo(destination.AsSpan());
        CollectionAssert.AreEquivalent(items, destination);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CopyToSpan_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        BiMap<int, string> biMap = new([new KeyValuePair<int, string>(1, "one")]);
        KeyValuePair<int, string>[] destination = [];
        biMap.CopyTo(destination.AsSpan());
    }

    [TestMethod]
    public void TryGetValuesSpan_ShouldReturnTrue_WhenAllKeysExist()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "one");
        biMap.Add(2, "two");
        int[] keys = [1, 2];
        string[] values = new string[2];
        bool ok = biMap.TryGetValues(keys, values);
        Assert.IsTrue(ok);
        CollectionAssert.AreEqual(new[] { "one", "two" }, values);
    }

    [TestMethod]
    public void TryGetValuesSpan_ShouldReturnFalse_WhenSpanTooSmall()
    {
        BiMap<int, string> biMap = new([
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ]);
        int[] keys = [1, 2];
        string[] values = new string[1];
        Assert.IsFalse(biMap.TryGetValues(keys, values));
    }

    [TestMethod]
    public void TryGetByKeyToSpan_ShouldReturnTrue_AndPopulate()
    {
        BiMap<string, int> biMap = [];
        biMap.Add("a", 100);
        Span<int> dest = stackalloc int[1];
        bool result = biMap.TryGetByKeyToSpan("a", dest);
        Assert.IsTrue(result);
        Assert.AreEqual(100, dest[0]);
    }

    [TestMethod]
    public void TryGetByKeyToSpan_ShouldReturnFalse_WhenKeyMissingOrSpanTooSmall()
    {
        BiMap<string, int> biMap = [];
        Span<int> dest = []; // too small
        Assert.IsFalse(biMap.TryGetByKeyToSpan("missing", dest));

        dest = new int[1];
        Assert.IsFalse(biMap.TryGetByKeyToSpan("missing", dest));
    }

    [TestMethod]
    public void TryGetMultipleByKeys_ShouldReturnTrue_WhenAllExist()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(1, "one");
        biMap.Add(2, "two");
        int[] keys = [1, 2];
        string[] vals = new string[2];
        Assert.IsTrue(biMap.TryGetMultipleByKeys(keys, vals));
        Assert.AreEqual("one", vals[0]);
        Assert.AreEqual("two", vals[1]);
    }

    [TestMethod]
    public void TryGetKeysToSpan_ShouldReturnTrue_WhenSpanSufficient()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(5, "five");
        biMap.Add(6, "six");
        Span<int> span = stackalloc int[2];
        Assert.IsTrue(biMap.TryGetKeysToSpan(span));
        CollectionAssert.AreEquivalent(new[] { 5, 6 }, span.ToArray());
    }

    [TestMethod]
    public void TryGetValuesToSpan_ShouldReturnTrue_WhenSpanSufficient()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(3, "three");
        biMap.Add(4, "four");
        string[] span = new string[2];
        Assert.IsTrue(biMap.TryGetValuesToSpan(span));
        CollectionAssert.AreEquivalent(new[] { "three", "four" }, span.ToArray());
    }

    [TestMethod]
    public async Task RemoveByValueAsync_ShouldRemoveValue_WhenExists()
    {
        BiMap<int, string> biMap = [];
        biMap.Add(8, "eight");
        bool result = await biMap.RemoveByValueAsync("eight");
        Assert.IsTrue(result);
        Assert.IsFalse(biMap.ContainsValue("eight"));
    }

    [TestMethod]
    public void CreateFromType_ShouldReturnEmptyBiMapOfGivenTypes()
    {
        BiMap<int, string> bm = BiMap<int, string>.CreateFromType(typeof(int), typeof(string));
        Assert.IsInstanceOfType(bm, typeof(BiMap<int, string>));
        Assert.AreEqual(0, bm.Count);
    }
    
        // --- Constructor and capacity behaviors ---
    [TestMethod]
    public void Constructor_WithNegativeCapacity_ShouldThrowArgumentOutOfRange()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new BiMap<int, string>(-1));
    }

    [TestMethod]
    public void TrimExcess_ShouldNotChangeCount()
    {
        BiMap<int, string> biMap = new BiMap<int, string>
        {
            { 1, "one" },
            { 2, "two" }
        };
        biMap.TrimExcess();
        Assert.AreEqual(2, biMap.Count);
        Assert.IsTrue(biMap.ContainsKey(1) && biMap.ContainsValue("two"));
    }

    // --- Add / TryAdd async cancellation ---
    [TestMethod]
    public async Task TryAddAsync_WithCanceledToken_ShouldThrowOperationCanceledException()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();
        BiMap<int, string> biMap = [];
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => biMap.TryAddAsync(1, "one", cts.Token).AsTask());
    }

    [TestMethod]
    public async Task RemoveByKeyAsync_WithCanceledToken_ShouldThrowOperationCanceledException()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();
        BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => biMap.RemoveByKeyAsync(1, cts.Token));
    }

    // --- RemoveByValueAsync and TryGetByKeyAsync ---
    [TestMethod]
    public async Task TryGetByKeyAsync_WhenMissing_ShouldReturnFalseAndDefault()
    {
        BiMap<int, string> biMap = [];
        (bool found, string val) = await biMap.TryGetByKeyAsync(123);
        Assert.IsFalse(found);
        Assert.IsNull(val);
    }

    // --- CopyTo array overload edge cases ---
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CopyTo_Array_NullArray_ShouldThrowArgumentNullException()
    {
        BiMap<int, string> biMap = new BiMap<int, string>([new KeyValuePair<int, string>(1, "one")]);
        biMap.CopyTo(null!, 0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void CopyTo_Array_NegativeIndex_ShouldThrowArgumentOutOfRange()
    {
        BiMap<int, string> biMap = new BiMap<int, string>([new KeyValuePair<int, string>(1, "one")]);
        KeyValuePair<int, string>[] arr = new KeyValuePair<int, string>[1];
        biMap.CopyTo(arr, -1);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CopyTo_Array_NotEnoughSpace_ShouldThrowArgumentException()
    {
        BiMap<int, string> biMap = new BiMap<int, string>([
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ]);
        KeyValuePair<int, string>[] arr = new KeyValuePair<int, string>[2];
        // start at index 1 leaves only 1 slot for 2 items
        biMap.CopyTo(arr, 1);
    }

    [TestMethod]
    public void CopyTo_Array_WithOffset_ShouldCopyCorrectly()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ];
        BiMap<int, string> biMap = new BiMap<int, string>(items);
        KeyValuePair<int, string>[] arr = new KeyValuePair<int, string>[3];
        biMap.CopyTo(arr, 1);
        // arr[1] and arr[2] should contain the two items
        Assert.AreEqual(1, arr[1].Key);
        Assert.AreEqual("one", arr[1].Value);
        Assert.AreEqual(2, arr[2].Key);
        Assert.AreEqual("two", arr[2].Value);
    }

    // --- Enumeration / Mutation during iteration ---
    [TestMethod]
    public void Enumerator_ModifyDuringIteration_ShouldThrowInvalidOperationException()
    {
        BiMap<int, string> biMap = new BiMap<int, string>
        {
            { 1, "one" },
            { 2, "two" }
        };
        using IEnumerator<KeyValuePair<int, string>> enumerator = biMap.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        biMap.Add(3, "three");
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void IEnumerable_GetEnumerator_ShouldEnumerateAllPairs()
    {
        KeyValuePair<int, string>[] items =
        [
            new KeyValuePair<int, string>(10, "ten"),
            new KeyValuePair<int, string>(20, "twenty")
        ];
        BiMap<int, string> biMap = new(items);
        List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>(((IEnumerable)biMap).Cast<KeyValuePair<int, string>>());
        CollectionAssert.AreEquivalent(items, list);
    }

    // --- Span overloads when keys/values missing or too small ---
    [TestMethod]
    public void TryGetValuesToSpan_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        BiMap<int, string> biMap = new BiMap<int, string>([
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ]);
        Span<string> dest = new string[1];
        Assert.IsFalse(biMap.TryGetValuesToSpan(dest));
    }

    [TestMethod]
    public void TryGetKeysToSpan_WhenSpanTooSmall_ShouldReturnFalse()
    {
        BiMap<int, string> biMap = new BiMap<int, string>([
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(2, "two")
        ]);
        Span<int> dest = stackalloc int[1];
        Assert.IsFalse(biMap.TryGetKeysToSpan(dest));
    }

    [TestMethod]
    public void CreateFromType_MultipleCalls_ShouldBeIndependentInstances()
    {
        BiMap<int, string> a = BiMap<int, string>.CreateFromType(typeof(int), typeof(string));
        BiMap<int, string> b = BiMap<int, string>.CreateFromType(typeof(int), typeof(string));
        Assert.AreNotSame(a, b);
        Assert.AreEqual(0, a.Count);
        Assert.AreEqual(0, b.Count);
    }

    [TestMethod]
    public void FromDictionary_ShouldProduceCorrectInverseLookup()
    {
        Dictionary<char, int> dict = new Dictionary<char, int> { ['a'] = 1, ['b'] = 2 };
        BiMap<char, int> biMap = BiMap<char, int>.FromDictionary(dict);
        Assert.AreEqual(2, (int)biMap['b']);
        Assert.AreEqual('a', (char)biMap[1]);
    }

    [TestMethod]
    public void JsonSerialize_AndDeserialize_ShouldRoundTripCorrectly()
    {
        BiMap<string, int> biMap = new BiMap<string, int>
        {
            { "x", 42 },
            { "y", 99 }
        };

        JsonSerializerOptions opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = false };
        string json = System.Text.Json.JsonSerializer.Serialize(biMap, opts);
        BiMap<string, int> deserialized = System.Text.Json.JsonSerializer.Deserialize<BiMap<string, int>>(json, opts);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(2, deserialized.Count);
        Assert.AreEqual(42, deserialized["x"]);
        Assert.AreEqual("y", deserialized[99]);
    }

    [TestMethod]
    public void AsICollection_RemovePair_ShouldWork()
    {
        BiMap<int, string> biMap = new BiMap<int, string> { { 5, "five" } };
        ICollection<KeyValuePair<int, string>> coll = biMap;
        bool removed = coll.Remove(new KeyValuePair<int, string>(5, "five"));
        Assert.IsTrue(removed);
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void AsICollection_IsReadOnly_ShouldBeFalse()
    {
        ICollection<KeyValuePair<int, string>> coll = new BiMap<int, string>();
        Assert.IsFalse(coll.IsReadOnly);
    }

    [TestMethod]
    public void Clear_AfterAddAndRemove_ShouldLeaveEmpty()
    {
        BiMap<int, string> biMap = new BiMap<int, string>
        {
            { 1, "one" },
            { 2, "two" }
        };
        biMap.RemoveByKey(1);
        biMap.Clear();
        Assert.AreEqual(0, biMap.Count);
        Assert.IsFalse(biMap.ContainsKey(2));
    }
    
    [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_WithNullKey_ShouldThrowArgumentNullException()
        {
            BiMap<string, string> biMap = new BiMap<string, string> { { null!, "value" } };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_WithNullValue_ShouldThrowArgumentNullException()
        {
            BiMap<string, string> biMap = new BiMap<string, string> { { "key", null! } };
        }

        [TestMethod]
        public void TryAdd_WithNullKey_ShouldThrowArgumentNullException()
        {
            BiMap<string, string> biMap = [];
            Assert.ThrowsException<ArgumentNullException>(() => biMap.TryAdd(null!, "value"));
        }

        [TestMethod]
        public void ToDictionary_IsIndependentClone()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            Dictionary<int, string> dict = biMap.ToDictionary();
            dict[2] = "two";
            Assert.IsFalse(biMap.ContainsKey(2));
        }

        [TestMethod]
        public void KeysCollection_IsReadOnly()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            ICollection<int> keys = biMap.Keys as ICollection<int>;
            Assert.IsNotNull(keys);
            Assert.IsTrue(keys.IsReadOnly);
            Assert.ThrowsException<NotSupportedException>(() => keys.Add(2));
        }

        [TestMethod]
        public void ValuesCollection_IsReadOnly()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            ICollection<string> values = biMap.Values as ICollection<string>;
            Assert.IsNotNull(values);
            Assert.IsTrue(values.IsReadOnly);
            Assert.ThrowsException<NotSupportedException>(() => values.Add("two"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FromDictionary_WithDuplicates_ShouldThrowArgumentException()
        {
            Dictionary<int, string> dict = new Dictionary<int, string> { {1, "one"}, {1, "uno"} };
            BiMap<int, string>.FromDictionary(dict);
        }

        [TestMethod]
        public void Enumerator_Reset_ShouldResetEnumerator()
        {
            KeyValuePair<int, string>[] items =
            [
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two")
            ];
            BiMap<int, string> biMap = new BiMap<int, string>(items);
            using IEnumerator<KeyValuePair<int, string>> enumerator = biMap.GetEnumerator();
            while (enumerator.MoveNext()) { }
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
        }

        [TestMethod]
        public void ICollectionCopyTo_ShouldCopyViaInterface()
        {
            KeyValuePair<int, string>[] items =
            [
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two")
            ];
            BiMap<int, string> biMap = new BiMap<int, string>(items);
            ICollection<KeyValuePair<int, string>> coll = biMap;
            KeyValuePair<int, string>[] array = new KeyValuePair<int, string>[2];
            coll.CopyTo(array, 0);
            CollectionAssert.AreEquivalent(items, array);
        }

        [TestMethod]
        public void ICollectionClear_ShouldClearMap()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            ICollection<KeyValuePair<int, string>> coll = biMap;
            coll.Clear();
            Assert.AreEqual(0, biMap.Count);
        }

        [TestMethod]
        public void ICollectionContains_ShouldTestPair()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            ICollection<KeyValuePair<int, string>> coll = biMap;
            Assert.IsTrue(coll.Contains(new KeyValuePair<int, string>(1, "one")));
            Assert.IsFalse(coll.Contains(new KeyValuePair<int, string>(2, "two")));
        }

        [TestMethod]
        public void TryGetMultipleByKeys_WithEmptyInputs_ShouldReturnTrue()
        {
            BiMap<int, string> biMap = [];
            int[] keys = [];
            string[] vals = [];
            Assert.IsTrue(biMap.TryGetMultipleByKeys(keys, vals));
        }

        [TestMethod]
        public void TryGetKeysToSpan_EmptyMapAndEmptySpan_ShouldReturnTrue()
        {
            BiMap<int, string> biMap = [];
            Span<int> span = [];
            Assert.IsTrue(biMap.TryGetKeysToSpan(span));
        }

        [TestMethod]
        public void CopyTo_EmptySpanAndEmptyMap_ShouldSucceed()
        {
            BiMap<int, string> biMap = [];
            KeyValuePair<int, string>[] span = [];
            biMap.CopyTo(span);
            Assert.AreEqual(0, span.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CopyTo_SpanTooSmall_ShouldThrowArgumentException()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            Span<KeyValuePair<int, string>> span = [];
            biMap.CopyTo(span);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddRange_WithExistingKey_ShouldThrowArgumentException()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            KeyValuePair<int, string>[] items = [new KeyValuePair<int, string>(1, "uno"), new KeyValuePair<int, string>(2, "two")
            ];
            biMap.AddRange(items);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddRange_WithExistingValue_ShouldThrowArgumentException()
        {
            BiMap<int, string> biMap = new BiMap<int, string> { { 1, "one" } };
            KeyValuePair<int, string>[] items = [new KeyValuePair<int, string>(2, "one"), new KeyValuePair<int, string>(3, "three")
            ];
            biMap.AddRange(items);
        }
        
        [TestMethod]
        public void ContainsKeyAndContainsValue_ShouldWork()
        {
            BiMap<int, string> biMap = [];
            biMap.Add(1, "one");
            Assert.IsTrue(biMap.ContainsKey(1));
            Assert.IsFalse(biMap.ContainsKey(2));
            Assert.IsTrue(biMap.ContainsValue("one"));
            Assert.IsFalse(biMap.ContainsValue("two"));
        }

        [TestMethod]
        public void Indexer_ByKey_ShouldReturnValue_OrThrow()
        {
            BiMap<int, string> biMap = [];
            biMap.Add(1, "one");
            Assert.AreEqual("one", biMap[1]);
            Assert.ThrowsException<KeyNotFoundException>(() => { string v = biMap[2]; });
        }

        [TestMethod]
        public void Indexer_ByValue_ShouldReturnKey_OrThrow()
        {
            BiMap<int, string> biMap = [];
            biMap.Add(1, "one");
            Assert.AreEqual(1, biMap["one"]);
            Assert.ThrowsException<KeyNotFoundException>(() => { int k = biMap["two"]; });
        }

        [TestMethod]
        public void RemoveByKey_ShouldReturnCorrectBooleanAndRemove()
        {
            BiMap<int, string> biMap = [];
            biMap.Add(1, "one");
            Assert.IsFalse(biMap.RemoveByKey(2));
            Assert.IsTrue(biMap.RemoveByKey(1));
            Assert.IsFalse(biMap.ContainsKey(1));
            Assert.IsFalse(biMap.ContainsValue("one"));
        }

        [TestMethod]
        public void RemoveByValue_ShouldReturnCorrectBooleanAndRemove()
        {
            BiMap<int, string> biMap = [];
            biMap.Add(1, "one");
            Assert.IsFalse(biMap.RemoveByValue("two"));
            Assert.IsTrue(biMap.RemoveByValue("one"));
            Assert.IsFalse(biMap.ContainsKey(1));
            Assert.IsFalse(biMap.ContainsValue("one"));
        }

        [TestMethod]
        public void Clear_ShouldEmptyMap()
        {
            BiMap<int, string> biMap = [];
            biMap.Add(1, "one");
            biMap.Clear();
            Assert.AreEqual(0, biMap.Count);
            Assert.IsFalse(biMap.ContainsKey(1));
            Assert.IsFalse(biMap.ContainsValue("one"));
        }
        
        [TestMethod]
    public void KeyType_ShouldReturnTypeOfKey()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        
        // Act
        Type keyType = biMap.KeyType;
        
        // Assert
        Assert.AreEqual(typeof(string), keyType);
    }
    
    [TestMethod]
    public void ValueType_ShouldReturnTypeOfValue()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        
        // Act
        Type valueType = biMap.ValueType;
        
        // Assert
        Assert.AreEqual(typeof(int), valueType);
    }
    
    [TestMethod]
    public void Add_KeyValuePair_ShouldAddPairToMap()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        KeyValuePair<string, int> pair = new KeyValuePair<string, int>("key1", 1);
        
        // Act
        biMap.Add(pair);
        
        // Assert
        Assert.AreEqual(1, biMap.Count);
        Assert.IsTrue(biMap.ContainsKey("key1"));
        Assert.IsTrue(biMap.ContainsValue(1));
        Assert.AreEqual(1, biMap["key1"]);
        Assert.AreEqual("key1", biMap[1]);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Add_KeyValuePair_WithExistingKey_ShouldThrowArgumentException()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        biMap.Add("key1", 1);
        KeyValuePair<string, int> pair = new KeyValuePair<string, int>("key1", 2);
        
        // Act
        biMap.Add(pair); // Should throw ArgumentException
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Add_KeyValuePair_WithExistingValue_ShouldThrowArgumentException()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        biMap.Add("key1", 1);
        KeyValuePair<string, int> pair = new KeyValuePair<string, int>("key2", 1);
        
        // Act
        biMap.Add(pair); // Should throw ArgumentException
    }
    
    [TestMethod]
    public void IEnumerable_NonGenericGetEnumerator_ShouldEnumerateAllPairs()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        biMap.Add("key1", 1);
        biMap.Add("key2", 2);
        biMap.Add("key3", 3);
        
        // Act
        IEnumerable enumerable = biMap;
        IEnumerator enumerator = enumerable.GetEnumerator();
        int count = 0;
        List<KeyValuePair<string, int>> pairs = [];
        
        // Assert
        while (enumerator.MoveNext())
        {
            count++;
            KeyValuePair<string, int> current = (KeyValuePair<string, int>)enumerator.Current;
            pairs.Add(current);
        }
        
        Assert.AreEqual(3, count);
        CollectionAssert.Contains(pairs, new KeyValuePair<string, int>("key1", 1));
        CollectionAssert.Contains(pairs, new KeyValuePair<string, int>("key2", 2));
        CollectionAssert.Contains(pairs, new KeyValuePair<string, int>("key3", 3));
    }
    
    [TestMethod]
    public void IEnumerable_GetEnumerator_ShouldBeDifferentFromGenericEnumerator()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        biMap.Add("key1", 1);
        
        // Act
        IEnumerable enumerable = biMap;
        IEnumerator nonGenericEnumerator = enumerable.GetEnumerator();
        IEnumerator<KeyValuePair<string, int>> genericEnumerator = biMap.GetEnumerator();
        
        // Assert
        Assert.AreNotSame(nonGenericEnumerator, genericEnumerator);
    }
    
    [TestMethod]
    public void IEnumerable_GetEnumerator_ShouldResetCorrectly()
    {
        // Arrange
        BiMap<string, int> biMap = [];
        biMap.Add("key1", 1);
        biMap.Add("key2", 2);
        
        // Act
        IEnumerable enumerable = biMap;
        IEnumerator enumerator = enumerable.GetEnumerator();
        
        // Move to the end
        while (enumerator.MoveNext()) { }
        
        // Reset and count again
        enumerator.Reset();
        int count = 0;
        while (enumerator.MoveNext())
        {
            count++;
        }
        
        // Assert
        Assert.AreEqual(2, count);
    }
    
    [TestMethod]
    public void Remove_ShouldReturnFalse_WhenKeyValuePairNotFound()
    {
        // Arrange
        BiMap<string, int> biMap = []; // Replace with actual type arguments
        string key = "test";
        int value = 0;
        KeyValuePair<string, int> pair = new KeyValuePair<string, int>(key, value);

        // Act
        bool result = biMap.Remove(pair);

        // Assert
        Assert.IsFalse(result, "Remove should return false when KeyValuePair does not exist.");
    }

    [TestMethod]
    public void Remove_ShouldReturnFalse_WhenKeyExistsButValueDiffers()
    {
        BiMap<string, int> biMap = [];
        string key = "Test";
        int value1 = 123;
        int value2 = 234;

        biMap.Add(key, value1);

        KeyValuePair<string, int> pair = new KeyValuePair<string, int>(key, value2);

        bool result = biMap.Remove(pair);

        Assert.IsFalse(result, "Remove should return false when the value does not match.");
    }

    [TestMethod]
    public void TryGetValues_ShouldReturnFalse_WhenKeyNotFound()
    {
        // Arrange
        var bimap = new BiMap<int, int>();
        bimap.Add(123, 234);
        bimap.Add(890, 789);

        ReadOnlySpan<int> keys = [123, 456]; // 456 does not exist in map
        Span<int> values = new int[2]; // Correct size to avoid false return by size check

        // Act
        bool result = bimap.TryGetValues(keys, values);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryGetMultipleByKeys_ShouldReturnFalse_WhenValuesSpanTooSmall()
    {
        // Arrange
        var bimap = new BiMap<int, int>();
        bimap.Add(1, 10);
        bimap.Add(2, 20);

        ReadOnlySpan<int> keys = new int[] { 1, 2 };
        Span<int> values = new int[1]; // Smaller than keys length

        // Act
        bool result = bimap.TryGetMultipleByKeys(keys, values);

        // Assert
        Assert.IsFalse(result); // Returns false because values span length < keys length
    }

    [TestMethod]
    public void TryGetMultipleByKeys_ShouldReturnFalse_WhenKeyNotFound()
    {
        // Arrange
        var bimap = new BiMap<int, int>();
        bimap.Add(1, 10);
        bimap.Add(2, 20);

        ReadOnlySpan<int> keys = new int[] { 1, 3 }; // Key 3 does not exist
        Span<int> values = new int[2]; // Correct size to pass first check

        // Act
        bool result = bimap.TryGetMultipleByKeys(keys, values);

        // Assert
        Assert.IsFalse(result); // Returns false because key 3 not found
    }
}