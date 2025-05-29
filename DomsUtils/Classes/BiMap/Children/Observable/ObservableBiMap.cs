using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DomsUtils.Classes.BiMap.Base;

namespace DomsUtils.Classes.BiMap.Children.Observable;

/// <summary>
/// Represents a bi-directional map with built-in mechanisms to notify observers of any changes in the collection.
/// Both keys and values in the map must be unique and support efficient lookup, addition, and removal operations.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary. Must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary. Must be non-nullable.</typeparam>
/// <remarks>
/// This class inherits from <see cref="BiMap{TKey, TValue}"/>, providing support for two-way mapping between keys and values.
/// It also implements <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/> for observing mutations to the collection.
/// Changes to the map, such as adding, removing, or replacing elements, trigger appropriate notification events.
/// </remarks>
public class ObservableBiMap<TKey, TValue> : BiMap<TKey, TValue>, IBiMap<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    where TKey : notnull
    where TValue : notnull
{
    /// <summary>
    /// Occurs when the collection changes.
    /// </summary>
    /// <remarks>
    /// The <see cref="CollectionChanged"/> event is raised whenever there is an addition,
    /// removal, replacement, or reset operation performed on the collection.
    /// This enables subscribers to react to changes in the collection and stay synchronized with its state.
    /// </remarks>
    /// <seealso cref="INotifyCollectionChanged"/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Event triggered whenever a property in the <see cref="ObservableBiMap{TKey, TValue}"/> is changed.
    /// This event is part of the implementation of the <see cref="INotifyPropertyChanged"/> interface,
    /// enabling external components to observe and respond to property change notifications for the object.
    /// </summary>
    /// <remarks>
    /// The event is typically raised through the <see cref="OnPropertyChanged(string)"/> method when
    /// a relevant property changes. Observers may use this event to refresh or update dependent functionality
    /// or UI presentation layers.
    /// </remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Represents an observable bi-directional map (or bi-map) that maintains
    /// a one-to-one correspondence between keys and values. Provides functionality
    /// to observe and notify changes in its collection through the INotifyCollectionChanged
    /// and INotifyPropertyChanged interfaces.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the bi-map. Keys must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the bi-map. Values must be non-nullable.</typeparam>
    /// <remarks>
    /// Changes to the collection (such as additions, removals, and updates) trigger
    /// the appropriate collection changed and property changed events.
    /// The events allow external components to react to changes in real-time.
    /// </remarks>
    public ObservableBiMap() { }

    /// <summary>
    /// Represents an observable bidirectional dictionary that allows for mapping keys to values and values to keys,
    /// while providing notifications for changes in the collection or properties.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary. Must not be null.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary. Must not be null.</typeparam>
    public ObservableBiMap(int capacity = 0) : base(capacity) { }

    /// <summary>
    /// Represents a bi-directional map that allows for efficient lookups from both keys to values
    /// and values to keys, while providing notifications for changes to its contents.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the bi-map. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the bi-map. Must be non-nullable.</typeparam>
    /// <remarks>
    /// This class inherits from the <see cref="BiMap{TKey, TValue}"/> class and implements both
    /// <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/> interfaces
    /// to provide collection change notifications and property change notifications for bindings.
    /// ObservableBiMap supports operations such as add, remove, replace, and clear, while notifying
    /// subscribers about changes to the collection or its items.
    /// </remarks>
    public ObservableBiMap(IEqualityComparer<TKey>? keyComparer = null, 
        IEqualityComparer<TValue>? valueComparer = null, 
        int capacity = 0) 
        : base(keyComparer, valueComparer, capacity) { }

    /// <summary>
    /// Represents an observable bidirectional map that provides notifications when changes occur to its collection or properties.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the map. Must not be null.</typeparam>
    /// <typeparam name="TValue">The type of values in the map. Must not be null.</typeparam>
    /// <remarks>
    /// Inherits behavior from <see cref="BiMap{TKey, TValue}"/> and implements <see cref="INotifyCollectionChanged"/>
    /// and <see cref="INotifyPropertyChanged"/> to enable change notifications. This class is particularly useful in scenarios
    /// requiring real-time updates, such as data binding in UI applications.
    /// </remarks>
    public ObservableBiMap(IEnumerable<KeyValuePair<TKey, TValue>> items) : base(items) { }

    public new TValue this[TKey key]
    {
        get
        {
            if (TryGetByKey(key, out TValue? value))
                return value;
            throw new KeyNotFoundException($"The key '{key}' was not found in the BiMap.");
        }
    }

    public new TKey this[TValue value]
    {
        get
        {
            if (TryGetByValue(value, out TKey? key))
                return key;
            throw new KeyNotFoundException($"The value '{value}' was not found in the BiMap.");
        }
    }

    public new int Count => base.Count;
    
    public new Type KeyType => typeof(TKey);

    public new Type ValueType => typeof(TValue);

    public new IEnumerable<TKey> Keys => base.Keys;

    public new IEnumerable<TValue> Values => base.Values;

    /// <summary>
    /// Adds a key-value pair to the map and notifies observers of the change.
    /// </summary>
    /// <param name="key">The key to add to the map. Must be unique within the map.</param>
    /// <param name="value">The value to add to the map. Must be unique within the map.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a duplicate key or value is provided.
    /// </exception>
    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
    }

    /// <summary>
    /// Adds the specified key-value pair to the ObservableBiMap and triggers necessary notifications.
    /// </summary>
    /// <param name="item">The key-value pair to add to the ObservableBiMap.</param>
    public new void Add(KeyValuePair<TKey, TValue> item)
    {
        base.Add(item);
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
    }

    public new void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
    
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");
    
        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("The number of elements in the source BiMap is greater than the available space from arrayIndex to the end of the destination array.");
    
        foreach (KeyValuePair<TKey, TValue> pair in this)
            array[arrayIndex++] = pair;
    }

    /// <summary>
    /// Removes a specified key-value pair from the bi-directional map.
    /// </summary>
    /// <param name="item">The key-value pair to remove from the map.</param>
    /// <returns>
    /// <c>true</c> if the specified key-value pair was successfully removed; otherwise, <c>false</c>.
    /// </returns>
    public new bool Remove(KeyValuePair<TKey, TValue> item)
    {
        bool result = base.Remove(item);
        if (result)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
        }
        return result;
    }

    // Keeping only one implementation of IsReadOnly
    public new bool IsReadOnly => false;

    /// Attempts to add the specified key and value to the bi-directional map.
    /// If the key or value already exists, the method returns false without adding them.
    /// <param name="key">The key to add to the map.</param>
    /// <param name="value">The value to associate with the key in the map.</param>
    /// <returns>
    /// True if the key-value pair was successfully added; otherwise, false if the key or value already exists.
    /// </returns>
    public new bool TryAdd(TKey key, TValue value)
    {
        bool result = base.TryAdd(key, value);
        if (result)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
        }
        return result;
    }

    public new bool ContainsValue(TValue value)
    {
        return base.ContainsValue(value);
    }

    /// <summary>
    /// Adds a collection of key-value pairs to the bi-directional map, ensuring no duplicates
    /// and raising appropriate collection and property change notifications.
    /// </summary>
    /// <param name="items">The collection of key-value pairs to be added.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if there are duplicate keys or values in the provided <paramref name="items"/>
    /// or if a key or value already exists in the map.
    /// </exception>
    public new void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        List<KeyValuePair<TKey, TValue>> itemsList = items.ToList();
        
        if (itemsList.Count == 0)
            return;
        
        base.AddRange(itemsList);
        
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
    }

    /// Attempts to asynchronously add a key-value pair to the bi-directional map.
    /// If the specified key or value is already present, the method will fail silently
    /// without throwing an exception or modifying the map.
    /// <param name="key">The key to add to the map. Must not be null.</param>
    /// <param name="value">The value to add to the map. Must not be null.</param>
    /// <param name="cancellationToken">A cancellation token to monitor for operation cancellation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The result of the task is
    /// a boolean indicating whether the key-value pair was added successfully.
    /// </returns>
    public new async ValueTask<bool> TryAddAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        bool result = await base.TryAddAsync(key, value, cancellationToken);
        if (result)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
        }
        return result;
    }

    public new Task<(bool exists, TValue? value)> TryGetByKeyAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return base.TryGetByKeyAsync(key, cancellationToken);
    }

    /// <summary>
    /// Removes a key-value pair from the bi-directional map based on the specified key.
    /// Notifies listeners of the change if the removal is successful.
    /// </summary>
    /// <param name="key">The key of the key-value pair to be removed.</param>
    /// <returns>
    /// True if the key-value pair was successfully removed; otherwise, false.
    /// </returns>
    public new bool RemoveByKey(TKey key)
    {
        if (TryGetByKey(key, out TValue? value))
        {
            KeyValuePair<TKey, TValue> item = new KeyValuePair<TKey, TValue>(key, value);
            bool result = base.RemoveByKey(key);
            if (result)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Keys));
                OnPropertyChanged(nameof(Values));
            }
            return result;
        }
        return false;
    }

    /// <summary>
    /// Removes an entry from the bi-directional map by its value.
    /// Triggers change notifications for collections and properties if removal is successful.
    /// </summary>
    /// <param name="value">The value to remove from the bi-directional map.</param>
    /// <returns>True if the value and its associated key were successfully removed; otherwise, false.</returns>
    public new bool RemoveByValue(TValue value)
    {
        if (TryGetByValue(value, out TKey? key))
        {
            KeyValuePair<TKey, TValue> item = new KeyValuePair<TKey, TValue>(key, value);
            bool result = base.RemoveByValue(value);
            if (result)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Keys));
                OnPropertyChanged(nameof(Values));
            }
            return result;
        }
        return false;
    }

    public new bool TryGetByKey(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return base.TryGetByKey(key, out value);
    }

    public new bool TryGetByValue(TValue value, [MaybeNullWhen(false)] out TKey key)
    {
        return base.TryGetByValue(value, out key);
    }

    public new bool ContainsKey(TKey key)
    {
        return base.ContainsKey(key);
    }

    /// <summary>
    /// Asynchronously removes an element with the specified key from the bi-directional map.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is true if the element with the specified key was successfully removed, otherwise false.
    /// </returns>
    public new async Task<bool> RemoveByKeyAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (TryGetByKey(key, out TValue? value))
        {
            KeyValuePair<TKey, TValue> item = new KeyValuePair<TKey, TValue>(key, value);
            bool result = await base.RemoveByKeyAsync(key, cancellationToken);
            if (result)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Keys));
                OnPropertyChanged(nameof(Values));
            }
            return result;
        }
        return false;
    }

    /// <summary>
    /// Asynchronously removes a key-value pair from the collection where the value matches the specified input.
    /// </summary>
    /// <param name="value">The value of the key-value pair to be removed.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating
    /// whether the removal was successful.</returns>
    public new async Task<bool> RemoveByValueAsync(TValue value, CancellationToken cancellationToken = default)
    {
        if (TryGetByValue(value, out TKey? key))
        {
            KeyValuePair<TKey, TValue> item = new KeyValuePair<TKey, TValue>(key, value);
            bool result = await base.RemoveByValueAsync(value, cancellationToken);
            if (result)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Keys));
                OnPropertyChanged(nameof(Values));
            }
            return result;
        }
        return false;
    }

    public new void CopyTo(Span<KeyValuePair<TKey, TValue>> destination)
    {
        if (destination.Length < Count)
            throw new ArgumentException("Destination span is too small.", nameof(destination));

        int i = 0;
        foreach (KeyValuePair<TKey, TValue> pair in ToDictionary())
            destination[i++] = pair;
    }

    public new bool TryGetValues(ReadOnlySpan<TKey> keys, Span<TValue> values)
    {
        if (keys.Length > values.Length)
            return false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (TryGetByKey(keys[i], out TValue? value))
                values[i] = value;
            else
                return false;
        }
        return true;
    }

    public new bool TryGetByKeyToSpan(TKey key, Span<TValue> destination)
    {
        if (destination.Length < 1)
            return false;

        if (!TryGetByKey(key, out TValue? value)) return false;
        destination[0] = value;
        return true;
    }

    public new bool TryGetMultipleByKeys(ReadOnlySpan<TKey> keys, Span<TValue> values)
    {
        return TryGetValues(keys, values); // same as TryGetValues
    }

    public new bool TryGetKeysToSpan(Span<TKey> destination)
    {
        if (destination.Length < Count)
            return false;

        int i = 0;
        foreach (TKey key in Keys)
            destination[i++] = key;
        return true;
    }

    public new bool TryGetValuesToSpan(Span<TValue> destination)
    {
        if (destination.Length < Count)
            return false;

        int i = 0;
        foreach (TValue value in Values)
            destination[i++] = value;
        return true;
    }

    /// <summary>
    /// Removes all key-value pairs from the <see cref="ObservableBiMap{TKey, TValue}"/> and raises notifications
    /// for collection and property changes.
    /// </summary>
    /// <remarks>
    /// This method clears the bidirectional map by removing all elements, resets the collection,
    /// and invokes change notifications for the properties: <see cref="BiMap{TKey,TValue}.Count"/>, <see cref="BiMap{TKey,TValue}.Keys"/>, and <see cref="BiMap{TKey,TValue}.Values"/>.
    /// </remarks>
    public new void Clear()
    {
        base.Clear();
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
    }

    public new bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return ContainsKey(item.Key) && TryGetByKey(item.Key, out TValue? value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }

    /// <summary>
    /// Replaces the value associated with the specified key in the map with a new value,
    /// ensuring the new value is not already present in the map.
    /// </summary>
    /// <param name="key">The key whose associated value needs to be replaced.</param>
    /// <param name="newValue">The new value to associate with the specified key.</param>
    /// <returns><c>true</c> if the replacement was successful; <c>false</c> if the key does not exist or the new value is already present.</returns>
    public bool Replace(TKey key, TValue newValue)
    {
        if (!ContainsKey(key) || ContainsValue(newValue))
            return false;

        base.RemoveByKey(key);
        base.Add(key, newValue);
        
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        OnPropertyChanged(nameof(Values));
        
        return true;
    }

    /// <summary>
    /// Replaces the existing key associated with the specified value with a new key in the bi-directional map.
    /// </summary>
    /// <param name="value">The value whose associated key needs to be replaced.</param>
    /// <param name="newKey">The new key to associate with the specified value.</param>
    /// <returns>
    /// <c>true</c> if the key was successfully replaced; <c>false</c> if the specified value does not exist in the map
    /// or if the new key already exists in the map.
    /// </returns>
    public bool ReplaceByValue(TValue value, TKey newKey)
    {
        if (!ContainsValue(value) || ContainsKey(newKey))
            return false;

        base.RemoveByValue(value);
        base.Add(newKey, value);
        
        OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        OnPropertyChanged(nameof(Keys));
        
        return true;
    }

    /// <summary>
    /// Raises the <see cref="CollectionChanged"/> event with the provided action and item.
    /// </summary>
    /// <param name="action">The action that caused the event. For example, <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/>.</param>
    /// <param name="item">The item that was added or removed. This is optional and may be default if the action does not involve a specific item.</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, 
        KeyValuePair<TKey, TValue> item = default)
    {
        NotifyCollectionChangedEventArgs args;
        
        switch (action)
        {
            case NotifyCollectionChangedAction.Add:
                args = new NotifyCollectionChangedEventArgs(action, item);
                break;
            case NotifyCollectionChangedAction.Remove:
                args = new NotifyCollectionChangedEventArgs(action, item);
                break;
            case NotifyCollectionChangedAction.Reset:
            default:
                args = new NotifyCollectionChangedEventArgs(action);
                break;
        }
        
        CollectionChanged?.Invoke(this, args);
    }

    /// Raises the PropertyChanged event to notify listeners that a property value has changed.
    /// <param name="propertyName">
    /// The name of the property that changed. This is optional and can be left null or empty,
    /// but providing the name improves the ability to identify the changed property.
    /// </param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (KeyValuePair<TKey, TValue> pair in ToDictionary())
            yield return pair;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}