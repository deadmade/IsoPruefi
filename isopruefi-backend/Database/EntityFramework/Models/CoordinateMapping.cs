using System.ComponentModel.DataAnnotations;

namespace Database.EntityFramework.Models;

/// <summary>
/// Stores geographic coordinates associated with postalcodes.
/// </summary>
public class CoordinateMapping
{
    /// <summary>
    ///  Gets or sets the postalcode which is also the unique identifier.
    /// </summary>
    [Key]
    public int PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the name of the location.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Gets or sets the latitude of the location.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude of the location.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the time the postalcode was last processed by the GetWeatherDataWorker.
    /// </summary>
    [DataType(DataType.DateTime)]
    public DateTime? LastUsed { get; set; } = null;

    /// <summary>
    /// Gets or sets the time until which the entry is locked.
    /// </summary>
    [DataType(DataType.DateTime)]
    public DateTime? LockedUntil { get; set; } = null;
}