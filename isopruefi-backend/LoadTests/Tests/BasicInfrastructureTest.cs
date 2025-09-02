using LoadTests.Infrastructure;
using NBomber.CSharp;

namespace LoadTests.Tests;

/// <summary>
/// Basic infrastructure test to verify TestContainers and factory setup
/// </summary>
[TestFixture]
public class BasicInfrastructureTest : LoadTestBase
{
    [Test]
    public void Test_Infrastructure_Setup()
    {
        // Test that all components are initialized
        Assert.That(Factory, Is.Not.Null, "Factory should be initialized");
        Assert.That(ApiClient, Is.Not.Null, "ApiClient should be initialized");
        
        // Test database connection
        Assert.That(Factory.DatabaseConnectionString, Is.Not.Empty, "Database connection string should not be empty");
        
        // Test InfluxDB setup
        Assert.That(Factory.InfluxDbUrl, Is.Not.Empty, "InfluxDB URL should not be empty");
        Assert.That(Factory.InfluxDbToken, Is.Not.Empty, "InfluxDB token should not be empty");
        
        // Test MQTT setup
        Assert.That(Factory.MqttPort, Is.GreaterThan(0), "MQTT port should be set");
        
        // Test API client base address
        var baseUrl = GetApiBaseUrl();
        Assert.That(baseUrl, Is.Not.Empty, "API base URL should not be empty");
    }
}