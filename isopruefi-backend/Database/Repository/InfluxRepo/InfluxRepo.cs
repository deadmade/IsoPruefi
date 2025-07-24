using System.Drawing;
using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Configuration;

namespace Database.Repository.InfluxRepo;

public class InfluxRepo :IInfluxRepo
{
    private InfluxDBClient _client;

    public InfluxRepo( IConfiguration configuration)
    {
        const string host = "http://localhost:8181";
        const string database = "IsoPruefi";

        var t = configuration["Influx:InfluxDBToken"];

        _client = new InfluxDBClient(host, t, database: database);
    }

    public async Task WriteSensorData(double measurement, string sensor, long timestamp)
    {
        var dateTimeUtc = DateTimeOffset
            .FromUnixTimeSeconds(timestamp)
            .UtcDateTime;

        var point = PointData.Measurement("temperature")
            .SetTag("sensor", sensor)
            .SetField("value", measurement)
            .SetTimestamp(dateTimeUtc);
        await _client.WritePointAsync(point: point);
    }

    public async Task WriteOutsideWeatherData(string place, string website, double temperature, DateTime timestamp)
    {
        var point = PointData.Measurement("outside_temperaturer")
            .SetTag("place", place)
            .SetTag("website", website)
            .SetField("value", temperature)
            .SetTimestamp(timestamp);

        await _client.WritePointAsync(point: point);
    }


}