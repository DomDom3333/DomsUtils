using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace DomsUtils.DataStructures.BiMap.Base;

/// <summary>
/// Represents a bidirectional mapping (BiMap) between keys and values,
/// ensuring unique associations in both directions. A key maps to a unique
/// value and vice versa, maintaining a one-to-one correspondence.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the BiMap, which must be non-null.</typeparam>
/// <typeparam name="TValue">The type of the values in the BiMap, which must be non-null.</typeparam>
[JsonConverter(typeof(Tooling.BiMapJsonConverterFactory))]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | 
                            DynamicallyAccessedMemberTypes.PublicMethods)]
public class BiMap<TKey, TValue> : IBiMap<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    /// <summary>
    /// Stores the mapping of keys to values in the bidirectional dictionary.
    /// </summary>
    private readonly Dictionary<TKey, TValue> _keyToValue;

    /// <summary>
    /// Represents the internal dictionary used to map values to their corresponding keys
    /// within the bidirectional map.
    /// </summary>
    private readonly Dictionary<TValue, TKey> _valueToKey;

    #region Constructors

    /// <summary>
    /// Represents a bidirectional dictionary that allows for mapping keys to values and values to keys.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the bidirectional dictionary. Must not be null.</typeparam>
    /// <typeparam name="TValue">The type of values in the bidirectional dictionary. Must not be null.</typeparam>
    public BiMap(int capacity = 0)
    {
        _keyToValue = new Dictionary<TKey, TValue>(capacity);
        _valueToKey = new Dictionary<TValue, TKey>(capacity);
    }

    /// <summary>
    /// Represents a bidirectional mapping between keys and values, ensuring a one-to-one relationship
    /// between TKey and TValue. Provides efficient lookups in both directions.
    /// </summary>
    /// <typeparam name="TKey">The type of keys, which must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of values, which must be non-nullable.</typeparam>
    public BiMap() : this(0)
    {
    }

    /// <summary>
    /// Represents a bidirectional map between keys of type <typeparamref name="TKey"/>
    /// and values of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the map. Must not be null.</typeparam>
    /// <typeparam name="TValue">The type of the values in the map. Must not be null.</typeparam>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The types are preserved through DynamicallyAccessedMembers")]
    public BiMap(IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null, int capacity = 0) 
        : this(capacity)
    {
        _keyToValue = keyComparer != null 
            ? new Dictionary<TKey, TValue>(capacity, keyComparer) 
            : new Dictionary<TKey, TValue>(capacity);
        
        _valueToKey = valueComparer != null 
            ? new Dictionary<TValue, TKey>(capacity, valueComparer) 
            : new Dictionary<TValue, TKey>(capacity);
    }

    /// <summary>
    /// Represents a bidirectional dictionary, allowing fast lookup from key to value and from value to key.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the BiMap.</typeparam>
    /// <typeparam name="TValue">The type of values in the BiMap.</typeparam>
    public BiMap(IEnumerable<KeyValuePair<TKey, TValue>> items) : this()
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        // Pre-size dictionaries if possible
        if (items is ICollection<KeyValuePair<TKey, TValue>> collection)
        {
            _keyToValue = new Dictionary<TKey, TValue>(collection.Count);
            _valueToKey = new Dictionary<TValue, TKey>(collection.Count);
        }
        
        // Check for duplicates during insertion
        foreach (KeyValuePair<TKey, TValue> pair in items)
        {
            if (_keyToValue.ContainsKey(pair.Key))
                throw new ArgumentException($"Duplicate key: {pair.Key}", nameof(items));
        
            if (_valueToKey.ContainsKey(pair.Value))
                throw new ArgumentException($"Duplicate value: {pair.Value}", nameof(items));
            
            _keyToValue.Add(pair.Key, pair.Value);
            _valueToKey.Add(pair.Value, pair.Key);
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of key/value pairs in the bidirectional map.
    /// </summary>
    public int Count => _keyToValue.Count;

    /// <summary>
    /// Gets a value indicating whether the BiMap is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets the value associated with the specified key from the <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key whose associated value is to be retrieved.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the specified key does not exist in the map.</exception>
    public TValue this[TKey key] => _keyToValue[key];

    /// <summary>
    /// Gets the value associated with the specified <typeparamref name="TValue"/>.
    /// </summary>
    /// <param name="value">The <typeparamref name="TValue"/> for which to retrieve the corresponding <typeparamref name="TKey"/>.</param>
    /// <returns>The <typeparamref name="TKey"/> associated with the specified <typeparamref name="TValue"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified <typeparamref name="TValue"/> is not found in the map.</exception>
    public TKey this[TValue value] => _valueToKey[value];

    /// <summary>
    /// Gets a collection containing the keys of the map.
    /// </summary>
    public IEnumerable<TKey> Keys => _keyToValue.Keys;

    /// <summary>
    /// Gets a collection containing the values in the map.
    /// </summary>
    public IEnumerable<TValue> Values => _keyToValue.Values;

    /// <summary>
    /// Gets the <see cref="Type"/> of the key in the bimap.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type KeyType => typeof(TKey);

    /// <summary>
    /// Gets the runtime type of the values stored in the map.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type ValueType => typeof(TValue);

    #endregion

    #region Basic Operations

    /// <summary>
    /// Adds a new bidirectional mapping to the <see cref="BiMap{TKey, TValue}"/> instance.
    /// </summary>
    /// <param name="key">The key to be added to the map.</param>
    /// <param name="value">The value to be associated with the specified key.</param>
    /// <exception cref="ArgumentException">Thrown when the key already exists in the map or when the value already exists in the map.</exception>
    public void Add(TKey key, TValue value)
    {
        if (_keyToValue.ContainsKey(key))
            throw new ArgumentException("Duplicate key", nameof(key));
        
        if (_valueToKey.ContainsKey(value))
            throw new ArgumentException("Duplicate value", nameof(value));

        _keyToValue.Add(key, value);
        _valueToKey.Add(value, key);
    }

    /// <summary>
    /// Adds the specified key-value pair to the BiMap.
    /// </summary>
    /// <param name="item">The key-value pair to add to the BiMap.</param>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Tries to add a new key-value pair to the <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key to add to the bidirectional map.</param>
    /// <param name="value">The value to associate with the specified key.</param>
    /// <returns>True if the key-value pair was successfully added; false if the key or value already exists in the map.</returns>
    public bool TryAdd(TKey key, TValue value)
    {
        if (!_keyToValue.TryAdd(key, value))
            return false;

        if (!_valueToKey.TryAdd(value, key))
        {
            _keyToValue.Remove(key);
            return false;
        }
    
        return true;
    }

    /// <summary>
    /// Removes the specified key-value pair from the internal dictionaries.
    /// </summary>
    /// <param name="key">The key to be removed.</param>
    /// <param name="value">The value to be removed.</param>
    /// <returns>Returns <c>true</c> if the key-value pair was successfully removed; otherwise, <c>false</c>.</returns>
    private bool RemoveInternal(TKey key, TValue value)
    {
        _keyToValue.Remove(key);
        _valueToKey.Remove(value);
        return true;
    }

    /// <summary>
    /// Removes the entry associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the entry to be removed.</param>
    /// <returns>True if the entry is successfully removed; otherwise, false.</returns>
    public bool RemoveByKey(TKey key)
    {
        if (_keyToValue.TryGetValue(key, out TValue? value))
        {
            return RemoveInternal(key, value);
        }
        return false;
    }

    /// <summary>
    /// Removes the entry associated with the specified value from the <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    /// <param name="value">The value of the entry to remove.</param>
    /// <returns>
    /// A boolean value indicating whether the removal was successful. Returns <c>true</c> if the entry was found and removed;
    /// otherwise, returns <c>false</c>.
    /// </returns>
    public bool RemoveByValue(TValue value)
    {
        if (_valueToKey.TryGetValue(value, out TKey? key))
        {
            return RemoveInternal(key, value);
        }
        return false;
    }

    /// <summary>
    /// Removes the specified key-value pair from the BiMap.
    /// </summary>
    /// <param name="item">The key-value pair to remove from the BiMap.</param>
    /// <returns>
    /// True if the key-value pair was successfully removed; otherwise, false.
    /// This method also returns false if the key-value pair was not found in the BiMap.
    /// </returns>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (_keyToValue.TryGetValue(item.Key, out TValue? value) && 
            EqualityComparer<TValue>.Default.Equals(value, item.Value))
        {
            return RemoveInternal(item.Key, item.Value);
        }
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be retrieved.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns><c>true</c> if the map contains an element with the specified key; otherwise, <c>false</c>.</returns>
    public bool TryGetByKey(TKey key, [MaybeNullWhen(false)] out TValue value) =>
        _keyToValue.TryGetValue(key, out value);

    /// <summary>
    /// Attempts to retrieve the key associated with the specified value.
    /// </summary>
    /// <param name="value">The value for which to find the corresponding key.</param>
    /// <param name="key">
    /// When this method returns, contains the key associated with the specified value if the value is found;
    /// otherwise, the default value for the type of the key parameter. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the key associated with the specified value is found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetByValue(TValue value, [MaybeNullWhen(false)] out TKey key) =>
        _valueToKey.TryGetValue(value, out key);

    /// <summary>
    /// Determines whether the <see cref="BiMap{TKey, TValue}"/> contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the map.</param>
    /// <returns><c>true</c> if the map contains an element with the specified key; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(TKey key) => _keyToValue.ContainsKey(key);

    /// <summary>
    /// Determines whether the BiMap contains the specified value.
    /// </summary>
    /// <param name="value">The value to locate in the BiMap.</param>
    /// <returns>
    /// <c>true</c> if the value is found in the BiMap; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsValue(TValue value) => _valueToKey.ContainsKey(value);

    /// <summary>
    /// Determines whether the <see cref="BiMap{TKey, TValue}"/> contains a specific key-value pair.
    /// </summary>
    /// <param name="item">The key-value pair to locate in the BiMap.</param>
    /// <returns>True if the specified key-value pair exists in the BiMap; otherwise, false.</returns>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _keyToValue.TryGetValue(item.Key, out TValue? value) && 
               EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }


    /// <summary>
    /// Removes all key-value pairs from the <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    public void Clear()
    {
        _keyToValue.Clear();
        _valueToKey.Clear();
    }

    /// <summary>
    /// Copies the elements of the <see cref="BiMap{TKey, TValue}"/> to a specified array, starting at the provided array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="BiMap{TKey, TValue}"/>.</param>
    /// <param name="arrayIndex">The zero-based index in the destination array at which copying begins.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="array"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is less than 0 or greater than the length of <paramref name="array"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the number of elements in the source <see cref="BiMap{TKey, TValue}"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination array is not long enough to copy all the items in the collection.");
            
        foreach (KeyValuePair<TKey, TValue> pair in _keyToValue)
        {
            array[arrayIndex++] = pair;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="BiMap{TKey, TValue}"/> collection.
    /// </summary>
    /// <returns>An enumerator for the <see cref="BiMap{TKey, TValue}"/> collection.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _keyToValue.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the BiMap collection.
    /// </summary>
    /// <returns>An enumerator for iterating through the BiMap collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Adds a collection of key-value pairs to the <see cref="BiMap{TKey, TValue}"/> instance.
    /// </summary>
    /// <param name="items">The collection of key-value pairs to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any key or value in <paramref name="items"/> already exists in the map.</exception>
    public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        
        // Convert to list first to avoid multiple enumeration
        List<KeyValuePair<TKey, TValue>> itemsList = [.. items];
        
        // Check for duplicates
        foreach (KeyValuePair<TKey, TValue> pair in itemsList)
        {
            if (_keyToValue.ContainsKey(pair.Key))
                throw new ArgumentException($"Duplicate key: {pair.Key}", nameof(items));
        
            if (_valueToKey.ContainsKey(pair.Value))
                throw new ArgumentException($"Duplicate value: {pair.Value}", nameof(items));
        }

        // Add all items
        foreach (KeyValuePair<TKey, TValue> pair in itemsList)
        {
            _keyToValue.Add(pair.Key, pair.Value);
            _valueToKey.Add(pair.Value, pair.Key);
        }
    }

    /// <summary>
    /// Reduces the capacity of the internal data structures of the <see cref="BiMap{TKey, TValue}"/>
    /// to minimize memory overhead, if no new elements are added.
    /// </summary>
    public void TrimExcess()
    {
        _keyToValue.TrimExcess();
        _valueToKey.TrimExcess();
    }

    #endregion

    #region Async Operations

    /// <summary>
    /// Attempts to add a key-value pair to the <see cref="BiMap{TKey, TValue}"/> asynchronously.
    /// </summary>
    /// <param name="key">The key to add to the bi-directional map.</param>
    /// <param name="value">The value to associate with the specified key.</param>
    /// <param name="cancellationToken">A token that allows the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// <see langword="true"/> if the key-value pair was added successfully; otherwise, <see langword="false"/>.</returns>
    public async ValueTask<bool> TryAddAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Allow for asynchronous operation
        cancellationToken.ThrowIfCancellationRequested();
    
        return TryAdd(key, value);
    }

    /// <summary>
    /// Asynchronously attempts to retrieve a value associated with the specified key in the <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="BiMap{TKey, TValue}"/>.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A tuple containing a boolean indicating if the key exists and the associated value if found;
    /// the default value of the type if the key does not exist.
    /// </returns>
    public async Task<(bool exists, TValue? value)> TryGetByKeyAsync(TKey key, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        if (_keyToValue.TryGetValue(key, out TValue? value))
            return (true, value);
        return (false, default);
    }

    /// <summary>
    /// Asynchronously removes a key-value pair by the specified key from the <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key of the key-value pair to remove.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
    /// The task result is <c>true</c> if the key-value pair was successfully removed; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> RemoveByKeyAsync(TKey key, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
    
        return RemoveByKey(key);
    }

    /// <summary>
    /// Asynchronously removes the specified value from the <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    /// <param name="value">The value to be removed from the map.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns <c>true</c> if the value was successfully removed; otherwise, <c>false</c>.</returns>
    public async Task<bool> RemoveByValueAsync(TValue value, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
    
        return RemoveByValue(value);
    }

    #endregion

    #region Span Operations

    /// <summary>
    /// Copies the elements of the <see cref="BiMap{TKey, TValue}"/> to the specified span starting at the first position.
    /// </summary>
    /// <param name="destination">The destination span where the elements will be copied.</param>
    /// <exception cref="ArgumentException">Thrown when the provided span does not have enough capacity to hold all elements in the <see cref="BiMap{TKey, TValue}"/>.</exception>
    public void CopyTo(Span<KeyValuePair<TKey, TValue>> destination)
    {
        if (destination.Length < Count)
            throw new ArgumentException("Destination span is not long enough", nameof(destination));
        
        int index = 0;
        foreach (KeyValuePair<TKey, TValue> pair in _keyToValue)
        {
            destination[index++] = pair;
        }
    }

    /// <summary>
    /// Attempts to retrieve the values associated with a sequence of keys, storing them in the provided span.
    /// </summary>
    /// <param name="keys">A read-only span containing the keys for which the values are to be retrieved.</param>
    /// <param name="values">A span to store the corresponding values. Must have a length greater than or equal to the number of keys.</param>
    /// <returns>
    /// True if all keys are found and their corresponding values are successfully stored in the span; otherwise, false.
    /// </returns>
    public bool TryGetValues(ReadOnlySpan<TKey> keys, Span<TValue> values)
    {
        if (values.Length < keys.Length)
            return false;
        
        for (int i = 0; i < keys.Length; i++)
        {
            if (!_keyToValue.TryGetValue(keys[i], out TValue? value))
                return false;
            
            values[i] = value;
        }
        
        return true;
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key and stores it in the provided span.
    /// </summary>
    /// <param name="key">The key for which to retrieve the associated value.</param>
    /// <param name="destination">
    /// A span in which to store the retrieved value. The span must have a length of at least 1.
    /// </param>
    /// <returns>
    /// <c>true</c> if the key exists and the value is successfully retrieved into the destination span;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetByKeyToSpan(TKey key, Span<TValue> destination)
    {
        if (destination.Length < 1)
            return false;
        
        if (_keyToValue.TryGetValue(key, out TValue? value))
        {
            destination[0] = value;
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Attempts to retrieve multiple values corresponding to the specified keys.
    /// </summary>
    /// <param name="keys">A span containing the keys to look up in the mapping.</param>
    /// <param name="values">A span to store the resulting values corresponding to the keys. Its length must be greater than or equal to the length of the keys span.</param>
    /// <returns>Returns true if all keys were successfully found and their corresponding values stored in the output span; otherwise, returns false.</returns>
    public bool TryGetMultipleByKeys(ReadOnlySpan<TKey> keys, Span<TValue> values)
    {
        if (values.Length < keys.Length)
            return false;
        
        for (int i = 0; i < keys.Length; i++)
        {
            if (!_keyToValue.TryGetValue(keys[i], out TValue? value))
                return false;
            
            values[i] = value;
        }
        
        return true;
    }

    /// <summary>
    /// Attempts to copy all keys from the map into the provided span.
    /// </summary>
    /// <param name="destination">The destination span where the keys will be copied.</param>
    /// <returns>
    /// <c>true</c> if all keys were successfully copied to the span;
    /// otherwise, <c>false</c> if the span's length is not sufficient to hold all keys.
    /// </returns>
    public bool TryGetKeysToSpan(Span<TKey> destination)
    {
        if (destination.Length < Count)
            return false;
        
        int index = 0;
        foreach (TKey key in _keyToValue.Keys)
        {
            destination[index++] = key;
        }
        
        return true;
    }

    /// <summary>
    /// Attempts to populate the specified span with all values in the map.
    /// </summary>
    /// <param name="destination">The span to store the values from the map.</param>
    /// <returns>
    /// Returns <c>true</c> if the span is large enough to contain all values; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValuesToSpan(Span<TValue> destination)
    {
        if (destination.Length < Count)
            return false;
        
        int index = 0;
        foreach (TValue value in _keyToValue.Values)
        {
            destination[index++] = value;
        }
        
        return true;
    }

    #endregion

    #region Trimming Support

    /// <summary>
    /// Creates a new instance of the <see cref="BiMap{TKey, TValue}"/> class using the specified key and value types.
    /// </summary>
    /// <param name="keyType">The type of the key elements.</param>
    /// <param name="valueType">The type of the value elements.</param>
    /// <returns>A new instance of the <see cref="BiMap{TKey, TValue}"/> class.</returns>
    /// <remarks>
    /// This method is designed to ensure trimming-safe reflection for type creation.
    /// Dynamically accessed members of the key and value types are preserved based on the <see cref="DynamicallyAccessedMembersAttribute"/> provided.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The types used with this BiMap are preserved through the DynamicallyAccessedMembers attribute")]
    [RequiresUnreferencedCode("BiMap reflection-based operations require the types to be preserved")]
    public static BiMap<TKey, TValue> CreateFromType(Type keyType, Type valueType)
    {
        // Implementation that uses reflection safely with trimming warnings
        return new BiMap<TKey, TValue>();
    }

    #endregion

    #region Serialization Support

    /// <summary>
    /// Converts the current <see cref="BiMap{TKey, TValue}"/> instance to a dictionary representation.
    /// </summary>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> that represents the key-value pairs in the current <see cref="BiMap{TKey, TValue}"/>.
    /// </returns>
    public Dictionary<TKey, TValue> ToDictionary() => new Dictionary<TKey, TValue>(_keyToValue);

    /// <summary>
    /// Creates a new <see cref="BiMap{TKey, TValue}"/> instance from the given dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary containing key-value pairs to initialize the BiMap with.</param>
    /// <returns>A new <see cref="BiMap{TKey, TValue}"/> initialized with the provided key-value pairs.</returns>
    public static BiMap<TK, TV> FromDictionary<TK, TV>(Dictionary<TK, TV> dictionary) 
        where TK : notnull 
        where TV : notnull    
    {
        BiMap<TK, TV> biMap = new BiMap<TK, TV>(dictionary.Count);
        foreach (KeyValuePair<TK, TV> pair in dictionary)
            biMap.Add(pair.Key, pair.Value);
        return biMap;
    }

    #endregion
}