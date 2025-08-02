using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Database.Repository.InfluxRepo;

/// <inheritdoc />
public class InfluxRepo : IInfluxRepo
{
    private readonly InfluxDBClient _client;
    private readonly ILogger<InfluxRepo> _logger;


    /// <summary>
    /// Constructor for the InfluxRepo class.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentException"></exception>
    public InfluxRepo(IConfiguration configuration, ILogger<InfluxRepo> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var database = configuration["Influx:InfluxDBDatabase"] ?? "IsoPruefi";

        var token = configuration["Influx:InfluxDBToken"] ?? configuration["Influx_InfluxDBToken"];
        var host = configuration["Influx:InfluxDBHost"] ?? configuration["Influx_InfluxDBHost"];

        if (string.IsNullOrEmpty(token)) throw new ArgumentException("InfluxDB token is not configured.");

        if (string.IsNullOrEmpty(host)) throw new ArgumentException("InfluxDB host is not configured.");

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

    /// <inheritdoc />
    public async Task WriteOutsideWeatherData(int place, string website, double temperature, DateTime timestamp)
    {
        try
        {
            var point = PointData.Measurement("outside_temperature")
                .SetTag("place", place.ToString())
                .SetTag("website", website)
                .SetDoubleField("value", temperature)
                .SetDoubleField("value_fahrenheit", temperature * 9 / 5 + 32)
                .SetTimestamp(timestamp);

            await _client.WritePointAsync(point);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error writing outside weather data to InfluxDB");
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<PointDataValues> GetOutsideWeatherData(DateTime start, DateTime end, string place)
    {
        try
        {
            var query =
                $"SELECT place, time, value FROM outside_temperature where place='{place}' AND time BETWEEN TIMESTAMP '{start:yyyy-MM-dd HH:mm:ss}' AND TIMESTAMP '{end:yyyy-MM-dd HH:mm:ss}'";

            return _client.QueryPoints(query);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving outside weather data from InfluxDB");
            throw;
        }
    }


    /// <inheritdoc />
    public IAsyncEnumerable<PointDataValues> GetSensorWeatherData(DateTime start, DateTime end)
    {
        try
        {
            var query =
                $"SELECT sensor, time, value FROM temperature WHERE time BETWEEN TIMESTAMP '{start:yyyy-MM-dd HH:mm:ss}' AND TIMESTAMP '{end:yyyy-MM-dd HH:mm:ss}'";

            return _client.QueryPoints(query);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving outside weather data from InfluxDB");
            throw;
        }
    }
}