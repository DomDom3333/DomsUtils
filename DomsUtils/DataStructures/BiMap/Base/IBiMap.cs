using System.Diagnostics.CodeAnalysis;

namespace DomsUtils.DataStructures.BiMap.Base;

public interface IBiMap<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    TValue this[TKey key] { get; }
    TKey this[TValue value] { get; }
    IEnumerable<TKey> Keys { get; }
    IEnumerable<TValue> Values { get; }
    Type KeyType { get; }
    Type ValueType { get; }

    void Add(TKey key, TValue value);
    bool TryAdd(TKey key, TValue value);
    bool RemoveByKey(TKey key);
    bool RemoveByValue(TValue value);
    bool TryGetByKey(TKey key, [MaybeNullWhen(false)] out TValue value);
    bool TryGetByValue(TValue value, [MaybeNullWhen(false)] out TKey key);
    bool ContainsKey(TKey key);
    bool ContainsValue(TValue value);
    void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items);

    // Asynchronous operations
    ValueTask<bool> TryAddAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
    Task<(bool exists, TValue? value)> TryGetByKeyAsync(TKey key, CancellationToken cancellationToken = default);
    Task<bool> RemoveByKeyAsync(TKey key, CancellationToken cancellationToken = default);
    Task<bool> RemoveByValueAsync(TValue value, CancellationToken cancellationToken = default);

    // Span and performance-oriented operations
    void CopyTo(Span<KeyValuePair<TKey, TValue>> destination);
    bool TryGetValues(ReadOnlySpan<TKey> keys, Span<TValue> values);
    bool TryGetByKeyToSpan(TKey key, Span<TValue> destination);
    bool TryGetMultipleByKeys(ReadOnlySpan<TKey> keys, Span<TValue> values);
    bool TryGetKeysToSpan(Span<TKey> destination);
    bool TryGetValuesToSpan(Span<TValue> destination);

    // Conversion and creation helpers
    Dictionary<TKey, TValue> ToDictionary();
}