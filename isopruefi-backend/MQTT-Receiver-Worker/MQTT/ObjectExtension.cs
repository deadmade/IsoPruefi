
using System.Text.Json;

namespace MQTT_Receiver_Worker.MQTT;

internal static class ObjectExtensions
{
    public static TObject DumpToConsole<TObject>(this TObject @object)
    {
        var output = "NULL";
        if (@object != null)
        {
            output = JsonSerializer.Serialize(@object, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        
        Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");
        return @object;
    }
}