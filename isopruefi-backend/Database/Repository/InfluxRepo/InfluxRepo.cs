using System.Drawing;
using InfluxDB3.Client;
using InfluxDB3.Client.Write;

namespace Database.Repository.InfluxRepo;

public class InfluxRepo :IInfluxRepo
{
    private InfluxDBClient _client;

    public InfluxRepo()
    {
        const string host = "http://localhost:8181";
        const string token = "";
        const string database = "IsoPruefi";

        _client = new InfluxDBClient(host, token, database: database);
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


}