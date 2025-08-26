using InfluxDB3.Client.Write;

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
    /// <param name="place">Name of the location</param>
    /// <param name="website">Name of the API</param>
    /// <param name="temperature">Temperature Value</param>
    /// <param name="timestamp">Timestamp</param>
    /// <param name="postalcode">Postalcode of the location</param>
    /// <returns></returns>
    Task WriteOutsideWeatherData(string place, string website, double temperature, DateTime timestamp, int postalcode);

    /// <summary>
    /// Saves all timestamps where the sensor was available.
    /// </summary>
    /// <param name="sensor">SensorId</param>
    /// <param name="timestamp">Unix Timestamp</param>
    /// <param name="sequence"></param>
    /// <returns></returns>
    Task WriteUptime(string sensor, long timestamp, int? sequence);

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

    /// <summary>
    /// Returns all timestamps for which the sensor was available.
    /// </summary>
    /// <param name="sensor">SensorId</param>
    /// <returns></returns>
    IAsyncEnumerable<PointDataValues> GetUptime(string sensor);
}