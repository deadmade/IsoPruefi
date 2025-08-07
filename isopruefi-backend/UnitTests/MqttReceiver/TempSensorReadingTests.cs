using System.Text.Json;
using FluentAssertions;
using MQTT_Receiver_Worker.MQTT.Models;

namespace UnitTests.MqttReceiver;

/// <summary>
/// Unit tests for the TempSensorReading model, verifying JSON serialization and property behavior.
/// </summary>
[TestFixture]
public class TempSensorReadingTests
{
    private JsonSerializerOptions _options;

    /// <summary>
    /// Sets up test fixtures and initializes JSON serialization options.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region JSON Serialization Tests

    /// <summary>
    /// Tests that a complete TempSensorReading object serializes correctly to JSON.
    /// </summary>
    [Test]
    public void Serialize_CompleteTempSensorReading_ProducesCorrectJson()
    {
        var reading = new TempSensorReading
        {
            Timestamp = 1234567890,
            Value = new double?[] { 25.5, 26.0 },
            Sequence = 42,
            Meta = new List<TempSensorReading>
            {
                new() { Timestamp = 1234567888, Value = new double?[] { 24.0 }, Sequence = 40 }
            }
        };

        var json = JsonSerializer.Serialize(reading, _options);

        json.Should().Contain("\"timestamp\":1234567890");
        json.Should().Contain("\"value\":[25.5,26]");
        json.Should().Contain("\"sequence\":42");
        json.Should().Contain("\"meta\":");
    }

    /// <summary>
    /// Tests that a minimal TempSensorReading object serializes correctly to JSON.
    /// </summary>
    [Test]
    public void Serialize_MinimalTempSensorReading_ProducesCorrectJson()
    {
        var reading = new TempSensorReading
        {
            Timestamp = 1234567890,
            Value = new double?[] { 25.5 }
        };

        var json = JsonSerializer.Serialize(reading, _options);

        json.Should().Contain("\"timestamp\":1234567890");
        json.Should().Contain("\"value\":[25.5]");
        json.Should().Contain("\"sequence\":null");
        json.Should().NotContain("\"meta\"");
    }

    #endregion

    #region JSON Deserialization Tests

    /// <summary>
    /// Tests that a complete JSON string deserializes correctly to TempSensorReading.
    /// </summary>
    [Test]
    public void Deserialize_CompleteJson_ProducesCorrectObject()
    {
        var json = """
                   {
                       "timestamp": "1234567890",
                       "value": [25.5, 26.0],
                       "sequence": 42,
                       "meta": [
                           {
                               "timestamp": "1234567888",
                               "value": [24.0],
                               "sequence": 40
                           }
                       ]
                   }
                   """;

        var reading = JsonSerializer.Deserialize<TempSensorReading>(json, _options);

        reading.Should().NotBeNull();
        reading!.Timestamp.Should().Be(1234567890);
        reading.Value.Should().BeEquivalentTo(new[] { 25.5, 26.0 });
        reading.Sequence.Should().Be(42);
        reading.Meta.Should().NotBeNull();
        reading.Meta.Should().HaveCount(1);
        reading.Meta![0].Timestamp.Should().Be(1234567888);
        reading.Meta[0].Value.Should().BeEquivalentTo(new[] { 24.0 });
        reading.Meta[0].Sequence.Should().Be(40);
    }

    /// <summary>
    /// Tests that a minimal JSON string deserializes correctly to TempSensorReading.
    /// </summary>
    [Test]
    public void Deserialize_MinimalJson_ProducesCorrectObject()
    {
        var json = """
                   {
                       "timestamp": "1234567890",
                       "value": [25.5]
                   }
                   """;

        var reading = JsonSerializer.Deserialize<TempSensorReading>(json, _options);

        reading.Should().NotBeNull();
        reading!.Timestamp.Should().Be(1234567890);
        reading.Value.Should().BeEquivalentTo(new[] { 25.5 });
        reading.Sequence.Should().BeNull();
        reading.Meta.Should().BeNull();
    }

    /// <summary>
    /// Tests that timestamp can be read from string format (JsonNumberHandling.AllowReadingFromString).
    /// </summary>
    [Test]
    public void Deserialize_TimestampAsString_ParsesCorrectly()
    {
        var json = """
                   {
                       "timestamp": "1234567890",
                       "value": [25.5]
                   }
                   """;

        var reading = JsonSerializer.Deserialize<TempSensorReading>(json, _options);

        reading.Should().NotBeNull();
        reading!.Timestamp.Should().Be(1234567890);
    }

    /// <summary>
    /// Tests that timestamp can be read from number format.
    /// </summary>
    [Test]
    public void Deserialize_TimestampAsNumber_ParsesCorrectly()
    {
        var json = """
                   {
                       "timestamp": 1234567890,
                       "value": [25.5]
                   }
                   """;

        var reading = JsonSerializer.Deserialize<TempSensorReading>(json, _options);

        reading.Should().NotBeNull();
        reading!.Timestamp.Should().Be(1234567890);
    }

    /// <summary>
    /// Tests that null values in the value array are handled correctly.
    /// </summary>
    [Test]
    public void Deserialize_NullValuesInArray_HandlesCorrectly()
    {
        var json = """
                   {
                       "timestamp": 1234567890,
                       "value": [25.5, null, 26.0]
                   }
                   """;

        var reading = JsonSerializer.Deserialize<TempSensorReading>(json, _options);

        reading.Should().NotBeNull();
        reading!.Value.Should().HaveCount(3);
        reading.Value![0].Should().Be(25.5);
        reading.Value[1].Should().BeNull();
        reading.Value[2].Should().Be(26.0);
    }

    /// <summary>
    /// Tests that null meta list is handled by the NullMetaListConverter.
    /// </summary>
    [Test]
    public void Deserialize_NullMetaList_ConvertedToNull()
    {
        var json = """
                   {
                       "timestamp": 1234567890,
                       "value": [25.5],
                       "meta": [null, null]
                   }
                   """;

        var reading = JsonSerializer.Deserialize<TempSensorReading>(json, _options);

        reading.Should().NotBeNull();
        reading!.Meta.Should().BeNull();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Tests that all properties can be set and retrieved correctly.
    /// </summary>
    [Test]
    public void Properties_SetAndGet_WorkCorrectly()
    {
        var reading = new TempSensorReading();
        var testTimestamp = 1234567890L;
        var testValue = new double?[] { 25.5, null, 26.0 };
        var testSequence = 42;
        var testMeta = new List<TempSensorReading>();

        reading.Timestamp = testTimestamp;
        reading.Value = testValue;
        reading.Sequence = testSequence;
        reading.Meta = testMeta;

        reading.Timestamp.Should().Be(testTimestamp);
        reading.Value.Should().BeSameAs(testValue);
        reading.Sequence.Should().Be(testSequence);
        reading.Meta.Should().BeSameAs(testMeta);
    }

    /// <summary>
    /// Tests that default property values are as expected.
    /// </summary>
    [Test]
    public void DefaultValues_AreCorrect()
    {
        var reading = new TempSensorReading();

        reading.Timestamp.Should().Be(0);
        reading.Value.Should().BeNull();
        reading.Sequence.Should().BeNull();
        reading.Meta.Should().BeNull();
    }

    #endregion
}