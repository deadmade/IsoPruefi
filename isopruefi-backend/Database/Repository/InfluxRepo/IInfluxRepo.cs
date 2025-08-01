﻿using InfluxDB3.Client.Write;

namespace Database.Repository.InfluxRepo;

/// <summary>
/// Interface for the InfluxDB repository.
/// </summary>
public interface IInfluxRepo
{
    /// <summary>
    /// Generates a point in the InfluxDB database with the given sensor data for the temperature measurement.
    /// </summary>
    /// <param name="measurement">Temperature Value</param>
    /// <param name="sensor">SensorId</param>
    /// <param name="timestamp">Unix Timestamp</param>
    /// <param name="sequence"></param>
    /// <returns></returns>
    Task WriteSensorData(double measurement, string sensor, long timestamp, int sequence);

    /// <summary>
    /// Generates a point in the InfluxDB database with the given outside weather data.
    /// </summary>
    /// <param name="place"></param>
    /// <param name="website"></param>
    /// <param name="temperature"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    Task WriteOutsideWeatherData(string place, string website, double temperature, DateTime timestamp);

    /// <summary>
    /// Retrieves outside weather data for a given place and time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="place">The location for which to retrieve data.</param>
    /// <returns>An async enumerable of weather data points.</returns>
    IAsyncEnumerable<PointDataValues> GetOutsideWeatherData(DateTime start, DateTime end, string place);

    /// <summary>
    /// Retrieves sensor weather data for a given time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <returns>An async enumerable of sensor weather data points.</returns>
    IAsyncEnumerable<PointDataValues> GetSensorWeatherData(DateTime start, DateTime end);
}