using System.Drawing;
using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Configuration;

namespace Database.Repository.InfluxRepo;

/// <inheritdoc />
public class InfluxRepo : IInfluxRepo
{
    private readonly InfluxDBClient _client;

    public InfluxRepo(IConfiguration configuration)
    {
        const string database = "IsoPruefi";

        var token = configuration["Influx:InfluxDBToken"];
        var host = configuration["Influx:InfluxDBHost"];

        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("InfluxDB token is not configured.");
        }

        if (string.IsNullOrEmpty(host))
        {
            throw new ArgumentException("InfluxDB host is not configured.");
        }

        _client = new InfluxDBClient(host, token, database: database);
    }


    /// <inheritdoc />
    public async Task WriteSensorData(double measurement, string sensor, long timestamp, int sequence)
    {
        var dateTimeUtc = DateTimeOffset
            .FromUnixTimeSeconds(timestamp)
            .UtcDateTime;

        var point = PointData.Measurement("temperature")
            .SetTag("sensor", sensor)
            .SetTag("sequence", sequence.ToString())
            .SetField("value", measurement)
            .SetTimestamp(dateTimeUtc);
        await _client.WritePointAsync(point);
    }
}