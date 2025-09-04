namespace Get_weatherData_worker;

/// <summary>
///     Data class for structuring weather data.
/// </summary>
public class WeatherData
{
    /// <summary>
    ///     Temperature of the weather data.
    /// </summary>
    public double Temperature { get; set; }
    
    /// <summary>
    ///     Time of the measurement.
    /// </summary>
    public DateTime Timestamp { get; set; }
}