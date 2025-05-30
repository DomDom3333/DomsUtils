using System.Text.Json;
using System.Text.Json.Serialization;

namespace DomsUtils.DataStructures.BiMap.Base.Tooling;

/// <summary>
/// A custom JSON converter for the BiMap class, enabling serialization and deserialization
/// between BiMap objects and JSON representations. This converter ensures the BiMap maintains
/// its bijective relationship by verifying that no duplicate values exist during deserialization.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the BiMap. Must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of the values in the BiMap. Must be non-nullable.</typeparam>
public class BiMapJsonConverter<TKey, TValue> : JsonConverter<BiMap<TKey, TValue>> 
    where TKey : notnull 
    where TValue : notnull
{
    /// <summary>
    /// Reads a JSON representation of a BiMap object and deserializes it into a <see cref="BiMap{TKey, TValue}"/> instance.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> used to read the JSON data.</param>
    /// <param name="typeToConvert">The target type expected by the deserialization operation.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to control the deserialization process.</param>
    /// <returns>The deserialized <see cref="BiMap{TKey, TValue}"/> instance, or null if the JSON content is invalid or empty.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is not in the expected format or contains duplicate values.</exception>
    public override BiMap<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            reader.Read(); // advance past the null token
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        try
        {
            // Deserialize to dictionary first
            Dictionary<TKey, TValue>? dictionary = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options);

            if (dictionary == null)
            {
                return null;
            }

            BiMap<TKey, TValue> biMap = new BiMap<TKey, TValue>(dictionary.Count);

            HashSet<TValue> seenValues = new HashSet<TValue>();
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (seenValues.Contains(pair.Value))
                {
                    throw new JsonException($"Duplicate value detected during deserialization: {pair.Value}");
                }

                seenValues.Add(pair.Value);
                biMap.Add(pair.Key, pair.Value);
            }

            return biMap;
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            throw new JsonException("Error deserializing BiMap", ex);
        }
    }

    /// <summary>
    /// Writes the specified <see cref="BiMap{TKey, TValue}"/> object as a JSON representation using the provided <see cref="Utf8JsonWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write the JSON data to.</param>
    /// <param name="value">The <see cref="BiMap{TKey, TValue}"/> object to be serialized.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use during serialization.</param>
    public override void Write(Utf8JsonWriter writer, BiMap<TKey, TValue> value, JsonSerializerOptions options)
    {
        // Simply write as a dictionary
        JsonSerializer.Serialize(writer, value.ToDictionary(), options);
    }
}