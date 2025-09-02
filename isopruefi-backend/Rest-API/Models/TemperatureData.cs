namespace Rest_API.Models;

/// <summary>
///     Represents an overview of temperature data for different locations.
/// </summary>
public class TemperatureDataOverview
{
    public List<SensorData> SensorData { get; set; }

    /// <summary>
    ///     Gets or sets the list of temperature data for the outside location.
    /// </summary>
    public List<TemperatureData> TemperatureOutside { get; set; }
}

public class SensorData
{
    public string SensorName { get; set; }
    public string Location { get; set; }
    public List<TemperatureData> TemperatureDatas { get; set; }
}

/// <summary>
///     Represents a single temperature data point with timestamp and value.
/// </summary>
public class TemperatureData
{
    /// <summary>
    ///     Gets or sets the timestamp of the temperature measurement.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the temperature value.
    /// </summary>
    public double Temperature { get; set; }

    public string Plausibility { get; set; }
}