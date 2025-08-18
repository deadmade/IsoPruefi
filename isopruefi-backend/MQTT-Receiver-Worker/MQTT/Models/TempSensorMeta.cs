using System.Text.Json.Serialization;

namespace MQTT_Receiver_Worker.MQTT.Models;

/// <summary>
/// Represents a recovered temperature sensor reading from a MQTT device.
/// </summary>
public class TempSensorMeta
{
    /// <summary>
    /// Gets or sets the timestamp when the data was recorded.
    /// </summary>
    [JsonPropertyName("t")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long[] Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the temperature values from the Sensor.
    /// </summary>
    [JsonPropertyName("v")]
    public double[] Value { get; set; }
    
    /// <summary>
    /// Gets or sets the sequence number of the reading.
    /// </summary>
    [JsonPropertyName("s")]
    public int[] Sequence { get; set; }
}