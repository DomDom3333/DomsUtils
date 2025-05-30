using System.Text.Json;
using DomsUtils.DataStructures.BiMap.Base;

namespace DomsUtils.DataStructures.BiMap.Extensions;

/// <summary>
/// Provides extension methods for serializing and deserializing instances of the BiMap class.
/// </summary>
public static class BiMapJsonExtensions
{
    /// <summary>
    /// Serializes the current instance of a BiMap into a JSON string representation.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used in the BiMap. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of values used in the BiMap. Must be non-nullable.</typeparam>
    /// <param name="biMap">The BiMap instance to be serialized.</param>
    /// <param name="options">Optional <see cref="JsonSerializerOptions"/> to customize serialization behavior.</param>
    /// <returns>A JSON string representing the serialized BiMap instance.</returns>
    public static string Serialize<TKey, TValue>(this BiMap<TKey, TValue> biMap, JsonSerializerOptions? options = null)
        where TKey : notnull
        where TValue : notnull
    {
        return JsonSerializer.Serialize(biMap, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="BiMap{TKey, TValue}"/> instance.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the BiMap. Must not be nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the BiMap. Must not be nullable.</typeparam>
    /// <param name="json">The JSON string to be deserialized.</param>
    /// <param name="options">
    /// Optional <see cref="JsonSerializerOptions"/> to control the deserialization behavior.
    /// </param>
    /// <returns>
    /// An instance of <see cref="BiMap{TKey, TValue}"/> if deserialization is successful, or null if deserialization fails.
    /// </returns>
    public static BiMap<TKey, TValue>? Deserialize<TKey, TValue>(string? json, JsonSerializerOptions? options = null)
        where TKey : notnull
        where TValue : notnull
    {
        if (json == null)
        {
            return null;
        }
        
        return JsonSerializer.Deserialize<BiMap<TKey, TValue>>(json, options);
    }
}