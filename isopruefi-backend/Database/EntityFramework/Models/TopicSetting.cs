using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.EntityFramework.Models;

public class TopicSetting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TopicSettingId { get; set; }
    public string DefaultTopicPath { get; set; } = "dhbw/ai/si2023/";
    public int GroupId { get; set; }
    public string SensorType { get; set; } = "temp";
    public string SensorName { get; set; }


    
}