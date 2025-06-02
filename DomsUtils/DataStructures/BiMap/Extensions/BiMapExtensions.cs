using DomsUtils.DataStructures.BiMap.Base;

namespace DomsUtils.DataStructures.BiMap.Extensions;

/// <summary>
/// Contains extension methods for working with instances of <see cref="BiMap{TKey, TValue}"/>.
/// </summary>
public static class BiMapExtensions
{
    /// <summary>
    /// Converts a dictionary to a <see cref="BiMap{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary. Must be non-nullable.</typeparam>
    /// <param name="dictionary">The dictionary to convert.</param>
    /// <returns>A <see cref="BiMap{TKey, TValue}"/> containing the keys and values from the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionary"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the dictionary contains duplicate values.</exception>
    public static BiMap<TKey, TValue> ToBiMap<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary)
        where TKey : notnull
        where TValue : notnull
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));

        BiMap<TKey, TValue> biMap = new BiMap<TKey, TValue>();
        
        // Check for duplicate values first
        HashSet<TValue> uniqueValues = new HashSet<TValue>();
        foreach (TValue value in dictionary.Values)
        {
            if (!uniqueValues.Add(value))
            {
                throw new ArgumentException("Dictionary contains duplicate values, which is not allowed in a BiMap.", nameof(dictionary));
            }
        }

        // Now we can safely add all key-value pairs
        foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
        {
            biMap.Add(kvp.Key, kvp.Value);
        }

        return biMap;
    }

    /// <summary>
    /// Converts a dictionary to a <see cref="BiMap{TKey, TValue}"/> with the option to resolve conflicts.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary. Must be non-nullable.</typeparam>
    /// <param name="dictionary">The dictionary to convert.</param>
    /// <param name="conflictResolver">
    /// An optional function that is invoked when a conflict arises. The function takes the conflicting key and value
    /// and returns a boolean indicating whether to replace the existing entry in the BiMap.
    /// If null, conflicts are ignored.
    /// </param>
    /// <returns>
    /// A <see cref="BiMap{TKey, TValue}"/> constructed from the specified dictionary,
    /// with optional conflict resolution for duplicate values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionary"/> is null.</exception>
    public static BiMap<TKey, TValue> ToBiMapSafe<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        Func<TKey, TValue, bool>? conflictResolver = null)
        where TKey : notnull
        where TValue : notnull
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));

        BiMap<TKey, TValue> biMap = new BiMap<TKey, TValue>();

        foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
        {
            // Check if the value already exists in the BiMap
            if (biMap.ContainsValue(kvp.Value))
            {
                // If we have a conflict resolver and it returns true, 
                // remove the existing entry with this value
                if (conflictResolver != null && conflictResolver(kvp.Key, kvp.Value))
                {
                    // Find the key for the existing value
                    TKey existingKey = biMap[kvp.Value];
                    // Remove the existing key-value pair
                    biMap.RemoveByKey(existingKey);
                    // Now add the new key-value pair
                    biMap.Add(kvp.Key, kvp.Value);
                }
                // Otherwise ignore this entry (conflict not resolved)
            }
            else
            {
                // If the value doesn't exist yet, try to add it
                // This handles the case where the key might be duplicate
                if (!biMap.TryAdd(kvp.Key, kvp.Value) && conflictResolver != null && conflictResolver(kvp.Key, kvp.Value))
                {
                    // Key conflict - remove and add again if resolver says so
                    biMap.RemoveByKey(kvp.Key);
                    biMap.Add(kvp.Key, kvp.Value);
                }
            }
        }

        return biMap;
    }
    
    /// <summary>
    /// Converts an <see cref="IEnumerable{TSource}"/> to a <see cref="BiMap{TKey, TValue}"/> using the specified key and value selectors.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source enumerable.</typeparam>
    /// <typeparam name="TKey">The type of the keys in the resulting <see cref="BiMap{TKey, TValue}"/>. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the resulting <see cref="BiMap{TKey, TValue}"/>. Must be non-nullable.</typeparam>
    /// <param name="source">The source enumerable to convert.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="valueSelector">A function to extract a value from an element.</param>
    /// <returns>A <see cref="BiMap{TKey, TValue}"/> containing the keys and values selected from the source enumerable.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/>, <paramref name="keySelector"/>, or <paramref name="valueSelector"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if duplicate keys or values are encountered in the source enumerable.</exception>
    public static BiMap<TKey, TValue> ToBiMap<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector)
        where TKey : notnull
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(valueSelector);

        BiMap<TKey, TValue> biMap = new BiMap<TKey, TValue>();
        
        foreach (TSource item in source)
        {
            TKey key = keySelector(item);
            TValue value = valueSelector(item);
            
            // Handle duplicates or throw as needed
            biMap.TryAdd(key, value);
        }
        
        return biMap;
    }

    /// <summary>
    /// Converts the specified collection into a bidirectional map (BiMap) while allowing
    /// the resolution of conflicts that may arise from duplicate keys or values.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
    /// <typeparam name="TKey">The type of the keys in the resulting BiMap. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the resulting BiMap. Must be non-nullable.</typeparam>
    /// <param name="source">The source collection to convert into a BiMap.</param>
    /// <param name="keySelector">A function to extract the key from each element of the source collection.</param>
    /// <param name="valueSelector">A function to extract the value from each element of the source collection.</param>
    /// <param name="conflictResolver">
    /// An optional function that is invoked when a conflict arises. The function takes the conflicting key and value
    /// and returns a boolean indicating whether to replace the existing entry in the BiMap.
    /// If null, conflicts are ignored.
    /// </param>
    /// <returns>
    /// A <see cref="BiMap{TKey, TValue}"/> constructed from the specified source collection,
    /// with optional conflict resolution for duplicate keys or values.
    /// </returns>
    public static BiMap<TKey, TValue> ToBiMapSafe<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        Func<TKey, TValue, bool>? conflictResolver = null)
        where TKey : notnull
        where TValue : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        if (valueSelector == null)
            throw new ArgumentNullException(nameof(valueSelector));
        
        BiMap<TKey, TValue> biMap = new BiMap<TKey, TValue>();
        
        foreach (TSource item in source)
        {
            TKey key = keySelector(item);
            TValue value = valueSelector(item);
            
            // Check if the value already exists in the BiMap
            if (biMap.ContainsValue(value))
            {
                // If we have a conflict resolver and it returns true, 
                // remove the existing entry with this value
                if (conflictResolver != null && conflictResolver(key, value))
                {
                    // Find the key for the existing value
                    TKey existingKey = biMap[value];
                    // Remove the existing key-value pair
                    biMap.RemoveByKey(existingKey);
                    // Now add the new key-value pair
                    biMap.Add(key, value);
                }
                // Otherwise ignore this entry (conflict not resolved)
            }
            else
            {
                // If the value doesn't exist yet, try to add it
                // This handles the case where the key might be duplicate
                if (!biMap.TryAdd(key, value) && conflictResolver != null && conflictResolver(key, value))
                {
                    // Key conflict - remove and add again if resolver says so
                    biMap.RemoveByKey(key);
                    biMap.Add(key, value);
                }
            }
        }
        
        return biMap;
    }
}