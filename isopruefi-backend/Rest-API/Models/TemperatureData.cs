namespace Rest_API.Models;

/// <summary>
/// Represents an overview of temperature data for different locations.
/// </summary>
public class TemperatureDataOverview
{
    /// <summary>
    /// Gets or sets the list of temperature data for the south location.
    /// </summary>
    public List<TemperatureData> TemperatureSouth { get; set; }

    /// <summary>
    /// Gets or sets the list of temperature data for the north location.
    /// </summary>
    public List<TemperatureData> TemperatureNord { get; set; }

    /// <summary>
    /// Gets or sets the list of temperature data for the outside location.
    /// </summary>
    public List<TemperatureData> TemperatureOutside { get; set; }
}

/// <summary>
/// Represents a single temperature data point with timestamp and value.
/// </summary>
public class TemperatureData
{
    /// <summary>
    /// Gets or sets the timestamp of the temperature measurement.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the temperature value.
    /// </summary>
    public double Temperature { get; set; }
}