using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.EntityFramework.Models;

/// <summary>
/// Represents the settings for a specific MQTT topic, including default path, group, and sensor information.
/// </summary>
// Represents the settings for a specific topic, including default path, group, and sensor information.
public class TopicSetting
{
    /// <summary>
    /// Gets or sets the unique identifier for the TopicSetting entity.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TopicSettingId { get; set; }

    /// <summary>
    /// Gets or sets the default MQTT topic path for this setting.
    /// </summary>
    [MaxLength(100)]
    public string DefaultTopicPath { get; set; } = "dhbw/ai/si2023/";

    /// <summary>
    /// Gets or sets the group identifier associated with this topic setting.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Gets or sets the type of sensor (e.g., temperature, humidity).
    /// </summary>
    [MaxLength(50)]
    public string SensorType { get; set; } = "temp";

    /// <summary>
    /// Gets or sets the name of the sensor.
    /// </summary>
    [MaxLength(50)]
    public string? SensorName { get; set; }
}