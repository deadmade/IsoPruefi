using System.Text.Json.Serialization;

namespace MQTT_Receiver_Worker.MQTT.Models;

/// <summary>
///     Represents a temperature sensor reading from an MQTT device.
/// </summary>
public class TempSensorReading
{
    /// <summary>
    ///     Gets or sets the timestamp when the reading was taken.
    ///     The value represents Unix time (seconds since epoch).
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the temperature values from the sensor.
    ///     An array is used as the sensor might provide multiple reading points.
    /// </summary>
    [JsonPropertyName("value")]
    public double?[]? Value { get; set; }

    /// <summary>
    ///     Gets or sets the sequence number of the reading.
    ///     Used to track the order of readings and detect missing data.
    /// </summary>
    [JsonPropertyName("sequence")]
    public int? Sequence { get; set; }

    /// <summary>
    ///     Gets or sets additional metadata associated with the sensor reading.
    ///     This includes information for the bulk insert
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TempSensorMeta? Meta { get; set; }
}