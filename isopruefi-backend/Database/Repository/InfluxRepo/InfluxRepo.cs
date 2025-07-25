﻿using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Configuration;

namespace Database.Repository.InfluxRepo;

/// <inheritdoc />
public class InfluxRepo : IInfluxRepo
{
    private readonly InfluxDBClient _client;


    /// <summary>
    /// Constructor for the InfluxRepo class.
    /// </summary>
    /// <param name="configuration"></param>
    /// <exception cref="ArgumentException"></exception>
    public InfluxRepo(IConfiguration configuration)
    {
        const string database = "IsoPruefi";

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
    public async Task WriteOutsideWeatherData(string place, string website, double temperature, DateTime timestamp)
    {
        var point = PointData.Measurement("outside_temperature")
            .SetTag("place", place)
            .SetTag("website", website)
            .SetField("value", temperature)
            .SetTimestamp(timestamp);

        await _client.WritePointAsync(point);
    }
}