using System.Text.Json;
using FluentAssertions;
using MQTT_Receiver_Worker;
using MQTT_Receiver_Worker.MQTT.Models;

namespace UnitTests.MqttReceiver;

/// <summary>
/// Unit tests for the NullMetaListConverter class, verifying JSON conversion logic for TempSensorReading lists.
/// </summary>
[TestFixture]
public class NullMetaListConverterTests
{
    private NullMetaListConverter _converter;
    private JsonSerializerOptions _options;

    /// <summary>
    /// Sets up test fixtures and initializes the converter with JSON options.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _converter = new NullMetaListConverter();
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { _converter }
        };
    }

    #region Read Method Tests

    /// <summary>
    /// Tests that a null JSON value is correctly converted to null.
    /// </summary>
    [Test]
    public void Read_NullJson_ReturnsNull()
    {
        var json = "null";

        var result = JsonSerializer.Deserialize<List<TempSensorReading>?>(json, _options);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that an empty JSON array is correctly converted to null.
    /// </summary>
    [Test]
    public void Read_EmptyArray_ReturnsNull()
    {
        var json = "[]";

        var result = JsonSerializer.Deserialize<List<TempSensorReading>?>(json, _options);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that an array containing only null values is correctly converted to null.
    /// </summary>
    [Test]
    public void Read_ArrayWithOnlyNulls_ReturnsNull()
    {
        var json = "[null, null, null]";

        var result = JsonSerializer.Deserialize<List<TempSensorReading>?>(json, _options);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that an array with valid TempSensorReading objects is preserved.
    /// </summary>
    [Test]
    public void Read_ArrayWithValidObjects_ReturnsArray()
    {
        var json = """
                   [
                       {
                           "timestamp": 1234567890,
                           "value": [25.5],
                           "sequence": 1
                       },
                       {
                           "timestamp": 1234567891,
                           "value": [26.0],
                           "sequence": 2
                       }
                   ]
                   """;

        var result = JsonSerializer.Deserialize<List<TempSensorReading>?>(json, _options);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].Timestamp.Should().Be(1234567890);
        result[0].Value.Should().BeEquivalentTo(new double?[] { 25.5 });
        result[0].Sequence.Should().Be(1);
        result[1].Timestamp.Should().Be(1234567891);
        result[1].Value.Should().BeEquivalentTo(new double?[] { 26.0 });
        result[1].Sequence.Should().Be(2);
    }

    /// <summary>
    /// Tests that an array with mixed null and valid objects is preserved.
    /// </summary>
    [Test]
    public void Read_ArrayWithMixedNullAndValidObjects_ReturnsArray()
    {
        var json = """
                   [
                       null,
                       {
                           "timestamp": 1234567890,
                           "value": [25.5],
                           "sequence": 1
                       },
                       null
                   ]
                   """;

        var result = JsonSerializer.Deserialize<List<TempSensorReading>?>(json, _options);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result![0].Should().BeNull();
        result[1].Should().NotBeNull();
        result[1]!.Timestamp.Should().Be(1234567890);
        result[2].Should().BeNull();
    }

    /// <summary>
    /// Tests that a single valid object in an array is preserved.
    /// </summary>
    [Test]
    public void Read_ArrayWithSingleValidObject_ReturnsArray()
    {
        var json = """
                   [
                       {
                           "timestamp": 1234567890,
                           "value": [25.5],
                           "sequence": 1
                       }
                   ]
                   """;

        var result = JsonSerializer.Deserialize<List<TempSensorReading>?>(json, _options);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Timestamp.Should().Be(1234567890);
    }

    #endregion

    #region Write Method Tests

    /// <summary>
    /// Tests that a null list is correctly serialized as null.
    /// </summary>
    [Test]
    public void Write_NullList_SerializesAsNull()
    {
        List<TempSensorReading>? list = null;

        var json = JsonSerializer.Serialize(list, _options);

        json.Should().Be("null");
    }

    /// <summary>
    /// Tests that an empty list is correctly serialized as an empty array.
    /// </summary>
    [Test]
    public void Write_EmptyList_SerializesAsEmptyArray()
    {
        var list = new List<TempSensorReading>();

        var json = JsonSerializer.Serialize(list, _options);

        json.Should().Be("[]");
    }

    /// <summary>
    /// Tests that a list with valid objects is correctly serialized.
    /// </summary>
    [Test]
    public void Write_ListWithValidObjects_SerializesCorrectly()
    {
        var list = new List<TempSensorReading>
        {
            new()
            {
                Timestamp = 1234567890,
                Value = new double?[] { 25.5 },
                Sequence = 1
            }
        };

        var json = JsonSerializer.Serialize(list, _options);

        json.Should().Contain("1234567890");
        json.Should().Contain("25.5");
        json.Should().Contain("1");
    }

    /// <summary>
    /// Tests that a list with null objects is correctly serialized.
    /// </summary>
    [Test]
    public void Write_ListWithNullObjects_SerializesCorrectly()
    {
        var list = new List<TempSensorReading?> { null, null };

        var json = JsonSerializer.Serialize(list, _options);

        json.Should().Be("[null,null]");
    }

    #endregion
}