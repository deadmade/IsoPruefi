namespace LoadTests.Infrastructure;

public class LoadTestSettings
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public MqttSettings MqttSettings { get; set; } = new();
    public DatabaseSettings DatabaseSettings { get; set; } = new();
    public LoadTestScenarios LoadTestScenarios { get; set; } = new();

    /// <summary>
    ///     Number of virtual sensors to simulate during load testing
    /// </summary>
    public int SensorCount { get; set; } = 10;

    /// <summary>
    ///     Number of messages each sensor should send per minute
    /// </summary>
    public int MessagesPerSensorPerMinute { get; set; } = 6; // One message every 10 seconds

    /// <summary>
    ///     Duration of the load test in minutes
    /// </summary>
    public int TestDurationMinutes { get; set; } = 2;

    /// <summary>
    ///     Maximum time to wait for data verification in minutes
    /// </summary>
    public int DataVerificationTimeoutMinutes { get; set; } = 3;

    /// <summary>
    ///     Minimum success rate for MQTT publishing (percentage)
    /// </summary>
    public double MinimumSuccessRate { get; set; } = 95.0;

    /// <summary>
    ///     Minimum data verification rate (percentage of sensors with data in database)
    /// </summary>
    public double MinimumDataVerificationRate { get; set; } = 80.0;

    /// <summary>
    ///     Base temperature for sensor readings (Celsius)
    /// </summary>
    public double BaseTemperature { get; set; } = 23.0;

    /// <summary>
    ///     Temperature variance for realistic readings (Celsius)
    /// </summary>
    public double TemperatureVariance { get; set; } = 5.0;

    /// <summary>
    ///     Whether to run recovery data tests
    /// </summary>
    public bool EnableRecoveryTests { get; set; } = true;

    /// <summary>
    ///     Percentage of sensors that should have recovery capability
    /// </summary>
    public double RecoverySensorPercentage { get; set; } = 33.0; // Every 3rd sensor
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