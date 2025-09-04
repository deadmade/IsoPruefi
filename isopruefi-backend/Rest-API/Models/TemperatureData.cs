namespace Rest_API.Models;

/// <summary>
///     Represents an overview of temperature data for different locations.
/// </summary>
public class TemperatureDataOverview
{
    /// <summary>
    ///     Gets or sets the list of Sensor data for the inside temperature.
    /// </summary>
    public List<SensorData>? SensorData { get; set; }

    /// <summary>
    ///     Gets or sets the list of temperature data for the outside location.
    /// </summary>
    public List<TemperatureData>? TemperatureOutside { get; set; }
}

/// <summary>
///     Represents an overview of sensor data.
/// </summary>
public class SensorData
{
    /// <summary>
    ///     Gets or sets the name of the sensor.
    /// </summary>
    public string? SensorName { get; set; }
    
    /// <summary>
    ///     Gets or sets the location of the sensor.
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    ///     Gets or sets the temperature data of the sensor.
    /// </summary>
    public List<TemperatureData>? TemperatureDatas { get; set; }
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

    /// <summary>
    ///     Gets or sets the plausibility of the temperature data.
    /// </summary>
    public string Plausibility { get; set; } = string.Empty;
}