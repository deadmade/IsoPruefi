namespace LoadTests.Infrastructure;

public class LoadTestSettings
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public MqttSettings MqttSettings { get; set; } = new();
    public DatabaseSettings DatabaseSettings { get; set; } = new();
    public LoadTestScenarios LoadTestScenarios { get; set; } = new();
}

public class MqttSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1884;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class DatabaseSettings
{
    public string PostgresConnectionString { get; set; } = string.Empty;
    public string InfluxUrl { get; set; } = string.Empty;
    public string InfluxToken { get; set; } = string.Empty;
    public string InfluxOrg { get; set; } = string.Empty;
    public string InfluxBucket { get; set; } = string.Empty;
}

public class LoadTestScenarios
{
    public int MqttSensorCount { get; set; } = 10000;
    public int RestApiUserCount { get; set; } = 1000;
    public int TestDurationMinutes { get; set; } = 10;
}