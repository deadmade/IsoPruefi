using System.Text.Json;

namespace LoadTests.Infrastructure;

public class TestDataGenerator
{
    private static readonly Random Random = new();
    private static readonly string[] Locations = { "Berlin", "Munich", "Hamburg", "Frankfurt", "Stuttgart" };

    private static readonly string[] SensorNames =
        { "temp_sensor_north", "temp_sensor_south", "temp_sensor_east", "temp_sensor_west" };

    public static string GenerateRandomLocation()
    {
        return Locations[Random.Next(Locations.Length)];
    }

    public static string GenerateRandomSensorName()
    {
        return SensorNames[Random.Next(SensorNames.Length)];
    }

    public static DateTime GenerateRandomDateTime(int daysBack = 30)
    {
        var start = DateTime.UtcNow.AddDays(-daysBack);
        var range = (DateTime.UtcNow - start).Days;
        return start.AddDays(Random.Next(range));
    }

    public static double GenerateRandomTemperature(double min = -10.0, double max = 35.0)
    {
        return Math.Round(Random.NextDouble() * (max - min) + min, 2);
    }

    public static string GenerateSensorDataPayload(string sensorId, double? temperature = null)
    {
        var temp = temperature ?? GenerateRandomTemperature();
        var payload = new
        {
            sensor_id = sensorId,
            temperature = temp,
            humidity = Math.Round(Random.NextDouble() * 100, 2),
            timestamp = DateTime.UtcNow.ToString("O"),
            battery_level = Random.Next(20, 100),
            signal_strength = Random.Next(-80, -30)
        };

        return JsonSerializer.Serialize(payload);
    }

    public static string GenerateMqttTopic(string sensorId)
    {
        return $"/sensors/{sensorId}/data";
    }

    public static string GenerateUniqueSensorId(int instanceId)
    {
        return $"sensor_{instanceId:D6}_{Random.Next(1000, 9999)}";
    }

    public static object GenerateRandomSensorData()
    {
        return new
        {
            SensorId = GenerateRandomSensorName(),
            Temperature = GenerateRandomTemperature(),
            Humidity = Math.Round(Random.NextDouble() * 100, 2),
            Timestamp = DateTime.UtcNow,
            Location = GenerateRandomLocation(),
            BatteryLevel = Random.Next(20, 100),
            SignalStrength = Random.Next(-80, -30)
        };
    }

    public static (DateTime start, DateTime end) GenerateRandomDateRange(int maxDaysBack = 7, int maxRangeHours = 24)
    {
        var end = DateTime.UtcNow.AddDays(-Random.Next(0, maxDaysBack));
        var start = end.AddHours(-Random.Next(1, maxRangeHours));
        return (start, end);
    }
}