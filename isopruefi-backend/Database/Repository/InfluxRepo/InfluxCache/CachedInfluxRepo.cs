using InfluxDB3.Client;
using InfluxDB3.Client.Query;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Database.Repository.InfluxRepo.InfluxCache;

/// <summary>
///     Cached implementation of InfluxDB repository that buffers writes when InfluxDB is unavailable.
///     Decorates the base InfluxRepo with write-through caching for data resilience.
/// </summary>
public class CachedInfluxRepo : IInfluxRepo
{
    private const string CACHE_KEY_PREFIX = "failed_influx_point:";
    private readonly InfluxDBClient _client;
    private readonly ILogger<CachedInfluxRepo> _logger;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    ///     Constructor for the CachedInfluxRepo class.
    /// </summary>
    /// <param name="configuration">Configuration for InfluxDB connection</param>
    /// <param name="memoryCache">Memory cache for buffering failed writes</param>
    /// <param name="logger">Logger instance</param>
    /// <exception cref="ArgumentException">Thrown when InfluxDB configuration is missing</exception>
    public CachedInfluxRepo(IConfiguration configuration, IMemoryCache memoryCache, ILogger<CachedInfluxRepo> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

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

        await WritePointWithCache(point, "sensor");
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

            await WritePointWithCache(point, "weather");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating outside weather data point");
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

    /// <summary>
    ///     Attempts to write a point to InfluxDB, caching it if the write fails.
    /// </summary>
    /// <param name="point">The PointData to write</param>
    /// <param name="dataType">Type of data for logging purposes (sensor/weather)</param>
    /// <param name="writeToCache"></param>
    private async Task WritePointWithCache(PointData point, string dataType)
    {
        try
        {
            await _client.WritePointAsync(point);
        }
        catch (Exception ex)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{dataType}:{Guid.NewGuid()}";
            var cacheExpiry = TimeSpan.FromHours(24);

            _memoryCache.Set(cacheKey, point, cacheExpiry);
        }
    }

    /// <summary>
    ///     Gets all cached PointData objects that failed to write to InfluxDB.
    ///     Used by background service for retry operations.
    /// </summary>
    /// <returns>Dictionary of cache keys and their corresponding PointData objects</returns>
    public Dictionary<object, PointData> GetCachedPoints()
    {
        var cachedPoints = new Dictionary<object, PointData>();

        if (_memoryCache is not MemoryCache memCache) return cachedPoints;

        foreach (var key in memCache.Keys) cachedPoints.Add(key, _memoryCache.Get<PointData>(key));

        return cachedPoints;
    }

    /// <summary>
    ///     Removes a cached point after successful retry.
    /// </summary>
    /// <param name="cacheKey">The cache key to remove</param>
    public void RemoveCachedPoint(object cacheKey)
    {
        _memoryCache.Remove(cacheKey);
        _logger.LogDebug("Removed cached point: {CacheKey}", cacheKey);
    }
}