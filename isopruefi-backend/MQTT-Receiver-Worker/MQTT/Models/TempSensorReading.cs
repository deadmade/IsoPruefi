using System.Text.Json.Serialization;

namespace MQTT_Receiver_Worker.MQTT.Models;

public class TempSensorReading
{
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Timestamp { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public double Value { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("sequence")]
    public int Sequence { get; set; }
}
