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
}