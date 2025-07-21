namespace Database.EntityFramework.Models;

public class TopicSetting
{
    public int TopicId { get; set; }
    public string DefaultTopicPath { get; set; } = "dhbw/ai/si2023/";
    public int GroupId { get; set; }
    public string SensorType { get; set; } = "temp";
    public string SensorName { get; set; }
    
}