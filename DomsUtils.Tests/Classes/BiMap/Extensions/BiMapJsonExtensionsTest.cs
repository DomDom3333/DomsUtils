using System;
using System.Text.Json;
using DomsUtils.Classes.BiMap.Base;
using DomsUtils.Classes.BiMap.Extensions;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DomsUtils.Tests.Classes.BiMap.Extensions;

[TestClass]
[TestSubject(typeof(BiMapJsonExtensions))]
public class BiMapJsonExtensionsTest
{
    [TestMethod]
    public void Serialize_ValidBiMap_ReturnsCorrectJson()
    {
        // Arrange
        var biMap = new BiMap<string, int>();
        biMap.Add("one", 1);
        biMap.Add("two", 2);
        var expectedJson = "{\"one\":1,\"two\":2}";

        // Act
        var json = biMap.Serialize();

        // Assert
        Assert.AreEqual(expectedJson, json);
    }

    [TestMethod]
    public void Serialize_EmptyBiMap_ReturnsEmptyJson()
    {
        // Arrange
        var biMap = new BiMap<string, int>();
        var expectedJson = "{}";

        // Act
        var json = biMap.Serialize();

        // Assert
        Assert.AreEqual(expectedJson, json);
    }

    [TestMethod]
    public void Serialize_BiMapWithCustomOptions_UsesOptions()
    {
        // Arrange
        var biMap = new BiMap<string, int>();
        biMap.Add("one", 1);
        var options = new JsonSerializerOptions { WriteIndented = true };

        // Act
        var json = biMap.Serialize(options);

        // Assert
        Assert.IsTrue(json.Contains(Environment.NewLine)); // Expecting indented JSON
    }

    [TestMethod]
    public void Deserialize_ValidJson_ReturnsCorrectBiMap()
    {
        // Arrange
        var json = "{\"one\":1,\"two\":2}";

        // Act
        var biMap = BiMapJsonExtensions.Deserialize<string, int>(json);

        // Assert
        Assert.IsNotNull(biMap);
        Assert.AreEqual(2, biMap.Count);
        Assert.AreEqual(1, biMap["one"]);
        Assert.AreEqual("two", biMap[2]);
    }

    [TestMethod]
    public void Deserialize_EmptyJson_ReturnsEmptyBiMap()
    {
        // Arrange
        var json = "{}";

        // Act
        var biMap = BiMapJsonExtensions.Deserialize<string, int>(json);

        // Assert
        Assert.IsNotNull(biMap);
        Assert.AreEqual(0, biMap.Count);
    }

    [TestMethod]
    public void Deserialize_InvalidJson_ThrowsException()
    {
        // Arrange
        var invalidJson = "{\"one\":}";

        // Act & Assert
        Assert.ThrowsException<JsonException>(() => { BiMapJsonExtensions.Deserialize<string, int>(invalidJson); });
    }

    [TestMethod]
    public void Deserialize_NullJson_ReturnsNull()
    {
        // Arrange
        string? nullJson = null;

        // Act
        var biMap = BiMapJsonExtensions.Deserialize<string, int>(nullJson!);

        // Assert
        Assert.IsNull(biMap);
    }

    [TestMethod]
    public void Deserialize_WithCustomOptions_Success()
    {
        // Arrange
        var json = "{\"one\":1}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var biMap = BiMapJsonExtensions.Deserialize<string, int>(json, options);

        // Assert
        Assert.IsNotNull(biMap);
        Assert.AreEqual(1, biMap.Count);
        Assert.AreEqual(1, biMap["one"]);
    }
}