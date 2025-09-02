namespace Database.Repository.InfluxRepo;

/// <summary>
///     Interface for the InfluxDB repository.
/// </summary>
public interface IInfluxRepo
{
    /// <summary>
    ///     Generates a point in the InfluxDB database with the given sensor data for the temperature measurement.
    /// </summary>
    /// <param name="measurement">Temperature Value</param>
    /// <param name="sensor">SensorId</param>
    /// <param name="timestamp">Unix Timestamp</param>
    /// <param name="sequence">Sequence number</param>
    /// <returns>A task that represents asynchronous saving of data.</returns>
    Task WriteSensorData(double measurement, string sensor, long timestamp, int sequence);

    /// <summary>
    ///     Generates a point in the InfluxDB database with the given outside weather data.
    /// </summary>
    /// <param name="place">Name of the city</param>
    /// <param name="website">Name of the API that provided the data</param>
    /// <param name="temperature">Temperature data</param>
    /// <param name="timestamp">Timestamp</param>
    /// <param name="postalcode">Associated postalcode</param>
    /// <returns>A task that represents asynchronous saving of data.</returns>
    Task WriteOutsideWeatherData(string place, string website, double temperature, DateTime timestamp, int postalcode);
    
    /// <summary>
    /// Function saving each timestamp at which the Arduino was available.
    /// </summary>
    /// <param name="sensor">Sensor name</param>
    /// <param name="timestamp">Timestamp</param>
    /// <returns>A task that represents asynchronous saving of data.</returns>
    public Task WriteUptime(string sensor, long timestamp);

    /// <summary>
    ///     Retrieves outside weather data for a given place and time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="place">The location for which to retrieve data.</param>
    /// <returns>An async enumerable containing object arrays of temperature values.</returns>
    IAsyncEnumerable<object?[]> GetOutsideWeatherData(DateTime start, DateTime end, string place);

    /// <summary>
    ///     Retrieves sensor weather data for a given time range.
    /// </summary>
    /// <param name="start">The start of the time range.</param>
    /// <param name="end">The end of the time range.</param>
    /// <param name="sensor">Sensor name.</param>
    /// <returns>An async enumerable containing object arrays of temperature values.</returns>
    IAsyncEnumerable<object?[]> GetSensorWeatherData(DateTime start, DateTime end, string sensor);

    /// <summary>
    /// Retrieves all times the Arduino was available.
    /// </summary>
    /// <param name="sensor">Sensor name</param>
    /// <returns>An async enumerable containing all data points.</returns>
    IAsyncEnumerable<PointDataValues> GetUptime(string sensor);
}