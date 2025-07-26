namespace Rest_API;

public class TemperatureDataOverview
{
    public List<TemperatureData> TemperatureSouth { get; set; }

    public List<TemperatureData> TemperatureNord { get; set; }

    public List<TemperatureData> TemperatureOutside { get; set; }
}

public class TemperatureData
{
    public DateTime Timestamp { get; set; }

    public double Temperature { get; set; }
}