using InfluxDB3.Client;
using InfluxDB3.Client.Query;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Database.Repository.InfluxRepo.Influx;

/// <inheritdoc />
public class InfluxRepo : IInfluxRepo
{
    private readonly InfluxDBClient _client;
    private readonly ILogger<InfluxRepo> _logger;


    /// <summary>
    ///     Constructor for the InfluxRepo class.
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
    public async Task WriteOutsideWeatherData(string place, string website, double temperature, DateTime timestamp,
        int postalcode)
    {
        try
        {
            var point = PointData.Measurement("outside_temperature")
                .SetTag("place", place)
                .SetTag("website", website)
                .SetDoubleField("value", temperature)
                .SetDoubleField("value_fahrenheit", temperature * 9 / 5 + 32)
                .SetIntegerField("postalcode", postalcode)
                .SetTimestamp(timestamp);

            await _client.WritePointAsync(point);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error writing outside weather data to InfluxDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task WriteUptime(string sensor, long timestamp)
    {
        try
        {
            var dateTimeUtc = DateTimeOffset
                .FromUnixTimeSeconds(timestamp)
                .UtcDateTime;

            var point = PointData.Measurement("uptime")
                .SetField("sensor", sensor)
                .SetTimestamp(dateTimeUtc);

            await _client.WritePointAsync(point);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error writing uptime into InfluxDB");
            throw;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<object?[]> GetOutsideWeatherData(DateTime start, DateTime end, string place)
    {
        var timespan = end - start;
        string query;
        IAsyncEnumerable<object?[]> result;

        string group;
        if (timespan.TotalHours < 24) group = "1m";
        else if (timespan.TotalDays < 30) group = "1h";
        else group = "1d";

        var bucket = TimeSpan.FromDays(2);
        var bucketStart = start;
        while (bucketStart < end)
        {
            var bucketEnd = bucketStart + bucket;
            if (bucketEnd > end) bucketEnd = end;

            query =
                $"SELECT MEAN(value) FROM outside_temperature where place='{place}' AND time >= '{bucketStart:yyyy-MM-dd HH:mm:ss}' AND time <= '{bucketEnd:yyyy-MM-dd HH:mm:ss}' GROUP BY time({group}) fill(none)";

            try
            {
                result = _client.Query(query, QueryType.InfluxQL);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving outside weather data from InfluxDB");
                throw;
            }

            await foreach (var row in result) yield return row;

            bucketStart = bucketEnd;
        }
    }


    /// <inheritdoc />
    public async IAsyncEnumerable<object?[]> GetSensorWeatherData(DateTime start, DateTime end, string sensor)
    {
        var timespan = end - start;
        string query;
        IAsyncEnumerable<object?[]> result;

        string group;
        if (timespan.TotalHours < 24) group = "1m";
        else if (timespan.TotalDays < 30) group = "1h";
        else group = "1d";

        var bucket = TimeSpan.FromDays(2);
        var bucketStart = start;
        while (bucketStart < end)
        {
            var bucketEnd = bucketStart + bucket;
            if (bucketEnd > end) bucketEnd = end;

            query =
                $"SELECT MEAN(value) FROM temperature where sensor='{sensor}' AND time >= '{bucketStart:yyyy-MM-dd HH:mm:ss}' AND time <= '{bucketEnd:yyyy-MM-dd HH:mm:ss}' GROUP BY time({group}) fill(none)";

            try
            {
                result = _client.Query(query, QueryType.InfluxQL);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving outside weather data from InfluxDB");
                throw;
            }

            await foreach (var row in result) yield return row;

            bucketStart = bucketEnd;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<PointDataValues> GetUptime(string sensor)
    {
        try
        {
            var query =
                $"SELECT sensor, time FROM uptime WHERE sensor = '{sensor}'";

            return _client.QueryPoints(query);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving outside weather data from InfluxDB");
            throw;
        }
    }
}