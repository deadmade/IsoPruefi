// ReSharper disable InconsistentNaming

namespace Database.EntityFramework.Enums;

/// <summary>
///     Represents the different types of sensors available in the system.
/// </summary>
public enum SensorType
{
    /// <summary>
    ///     Temperature sensor.
    /// </summary>
    temp,

    /// <summary>
    ///     Sound pressure level sensor.
    /// </summary>
    spl,

    /// <summary>
    ///     Humidity sensor.
    /// </summary>
    hum,

    /// <summary>
    ///     IKEA sensor.
    /// </summary>
    ikea,

    /// <summary>
    ///     CO2 sensor.
    /// </summary>
    co2,

    /// <summary>
    ///     Microphone sensor.
    /// </summary>
    mic
}