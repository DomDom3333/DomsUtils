using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DomsUtils.Classes.BiMap.Base;
using DomsUtils.Classes.BiMap.Base.Tooling;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DomsUtils.Tests.Classes.BiMap.Base.Tooling;

[TestClass]
[TestSubject(typeof(BiMapJsonConverter<,>))]
public class BiMapJsonConverterTest
{
    private class TestKeyComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(string obj) => obj.ToLowerInvariant().GetHashCode();
    }

    private class TestValueComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => obj.GetHashCode();
    }

    [TestMethod]
    public void Read_ValidJson_ReturnsBiMap()
    {
        // Arrange
        string validJson = "{\"key1\":1,\"key2\":2,\"key3\":3}";
        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(validJson));
        reader.Read();

        // Act
        var converter = new BiMapJsonConverter<string, int>();
        var result = converter.Read(ref reader, typeof(BiMap<string, int>), options);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.ContainsKey("key1"));
        Assert.IsTrue(result.ContainsKey("key2"));
        Assert.IsTrue(result.ContainsKey("key3"));
    }

    [TestMethod]
    public void Read_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        string invalidJson = "\"notAnObject\"";
        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var converter = new BiMapJsonConverter<string, int>();

        // Act & Assert
        Assert.ThrowsException<JsonException>(() =>
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(invalidJson));
            converter.Read(ref reader, typeof(BiMap<string, int>), options);
        });
    }

    [TestMethod]
    public void Read_WithDuplicateValues_ThrowsJsonException()
    {
        // Arrange
        string jsonWithDuplicates = "{\"key1\":1,\"key2\":1,\"key3\":3}";
        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var converter = new BiMapJsonConverter<string, int>();

        // Act & Assert
        Assert.ThrowsException<JsonException>(() =>
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(jsonWithDuplicates));
            converter.Read(ref reader, typeof(BiMap<string, int>), options);
        });
    }

    [TestMethod]
    public void Write_ValidBiMap_WritesJson()
    {
        // Arrange
        var biMap = new BiMap<string, int>(new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 2 },
            { "key3", 3 }
        });

        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(buffer);

        // Act
        var converter = new BiMapJsonConverter<string, int>();
        converter.Write(writer, biMap, options);
        writer.Flush();

        // Assert
        string json = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
        string expectedJson = "{\"key1\":1,\"key2\":2,\"key3\":3}";
        Assert.AreEqual(expectedJson, json);
    }

    [TestMethod]
    public void Read_NullJson_ReturnsNull()
    {
        // Arrange
        string nullJson = "null";
        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var bytes = System.Text.Encoding.UTF8.GetBytes(nullJson);
        var reader = new Utf8JsonReader(bytes);
        reader.Read();

        // Act
        var converter = new BiMapJsonConverter<string, int>();
        var result = converter.Read(ref reader, typeof(BiMap<string, int>), options);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Write_EmptyBiMap_WritesEmptyJson()
    {
        // Arrange
        var biMap = new BiMap<string, int>();
        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(buffer);

        // Act
        var converter = new BiMapJsonConverter<string, int>();
        converter.Write(writer, biMap, options);
        writer.Flush();

        // Assert
        string json = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
        string expectedJson = "{}";
        Assert.AreEqual(expectedJson, json);
    }

    [TestMethod]
    public void Read_WithCustomComparers_ReturnsBiMap()
    {
        // Arrange
        string validJson = "{\"KEY1\":1,\"KEY2\":2}";
        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var converter = new BiMapJsonConverter<string, int>();
        var biMap = new BiMap<string, int>(new TestKeyComparer(), new TestValueComparer());
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(validJson));
        reader.Read();

        // Act
        var result = converter.Read(ref reader, biMap.GetType(), options);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void Read_InvalidJsonStructure_ThrowsJsonException()
    {
        // Arrange
        string invalidJson = "{\"key1\":1,\"key2\":[2]}";
        var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
        var converter = new BiMapJsonConverter<string, int>();

        // Act & Assert
        Assert.ThrowsException<JsonException>(() =>
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(invalidJson));
            converter.Read(ref reader, typeof(BiMap<string, int>), options);
        });
    }

[TestMethod]
public void Read_InvalidValueType_ThrowsJsonException()
{
    // Arrange
    string json = "{\"a\":1,\"b\":[2]}";  // 'b' is an array, not an int
    var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
    reader.Read(); // StartObject
    var converter = new BiMapJsonConverter<string, int>();

    // Act & Assert
    try
    {
        converter.Read(ref reader, typeof(BiMap<string, int>), options);
        Assert.Fail("Expected a JsonException due to invalid value type.");
    }
    catch (JsonException)
    {
        // success
    }
}

[TestMethod]
public void Read_DuplicateValues_ThrowsJsonException()
{
    // Arrange
    string json = "{\"a\":1,\"b\":1}";  // duplicate value '1'
    var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
    reader.Read(); // StartObject
    var converter = new BiMapJsonConverter<string, int>();

    // Act & Assert
    try
    {
        converter.Read(ref reader, typeof(BiMap<string, int>), options);
        Assert.Fail("Expected a JsonException due to duplicate values.");
    }
    catch (JsonException)
    {
        // success
    }
}

[TestMethod]
public void Read_InvalidToken_ThrowsJsonException()
{
    // Arrange
    string json = "\"justAString\"";  // not an object or null
    var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
    reader.Read(); // moves to String
    var converter = new BiMapJsonConverter<string, int>();

    // Act & Assert
    try
    {
        converter.Read(ref reader, typeof(BiMap<string, int>), options);
        Assert.Fail("Expected a JsonException due to invalid JSON token.");
    }
    catch (JsonException)
    {
        // success
    }
}

[TestMethod]
public void Read_JsonNullDictionary_ReturnsNull()
{
    // Arrange
    string json = "null"; // JSON null literal
    var options = new JsonSerializerOptions { Converters = { new BiMapJsonConverter<string, int>() } };
    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
    reader.Read(); // moves to JsonTokenType.Null
    var converter = new BiMapJsonConverter<string, int>();

    // Act
    BiMap<string, int>? result = converter.Read(ref reader, typeof(BiMap<string, int>), options);

    // Assert
    Assert.IsNull(result, "Expected the converter to return null for JSON null dictionary.");
}
}