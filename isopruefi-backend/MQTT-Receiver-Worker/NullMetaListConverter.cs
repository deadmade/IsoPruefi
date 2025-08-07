using System.Text.Json;
using System.Text.Json.Serialization;
using MQTT_Receiver_Worker.MQTT.Models;

namespace MQTT_Receiver_Worker;

/// <summary>
/// Custom JSON converter for List&lt;TempSensorReading&gt; that converts lists containing only null values to null.
/// This converter ensures that during JSON deserialization, if a meta list contains only null entries,
/// the entire list is set to null instead of maintaining a list of null objects.
/// </summary>
public class NullMetaListConverter : JsonConverter<List<TempSensorReading>?>
{
    /// <summary>
    /// Reads and converts the JSON to a List&lt;TempSensorReading&gt;.
    /// If the resulting list contains only null values, returns null instead of the list.
    /// </summary>
    /// <param name="reader">The Utf8JsonReader to read from</param>
    /// <param name="typeToConvert">The type to convert (List&lt;TempSensorReading&gt;?)</param>
    /// <param name="options">The JsonSerializerOptions to use during deserialization</param>
    /// <returns>
    /// A List&lt;TempSensorReading&gt; containing the deserialized values, or null if the list
    /// was empty, null, or contained only null values
    /// </returns>
    public override List<TempSensorReading>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        // Create new options without this converter to avoid infinite recursion
        var newOptions = new JsonSerializerOptions(options);
        for (var i = newOptions.Converters.Count - 1; i >= 0; i--)
            if (newOptions.Converters[i] is NullMetaListConverter)
                newOptions.Converters.RemoveAt(i);

        var list = JsonSerializer.Deserialize<List<TempSensorReading>?>(ref reader, newOptions);

        if (list == null || list.Count == 0)
            return null;

        // Check if all items are null
        if (list.All(item => item == null))
            return null;

        return list;
    }

    /// <summary>
    /// Writes the List&lt;TempSensorReading&gt; to JSON.
    /// Uses the default serialization behavior without any custom logic.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write to</param>
    /// <param name="value">The List&lt;TempSensorReading&gt; value to serialize</param>
    /// <param name="options">The JsonSerializerOptions to use during serialization</param>
    public override void Write(Utf8JsonWriter writer, List<TempSensorReading>? value, JsonSerializerOptions options)
    {
        // Create new options without this converter to avoid infinite recursion
        var newOptions = new JsonSerializerOptions(options);
        for (var i = newOptions.Converters.Count - 1; i >= 0; i--)
            if (newOptions.Converters[i] is NullMetaListConverter)
                newOptions.Converters.RemoveAt(i);

        JsonSerializer.Serialize(writer, value, newOptions);
    }
}