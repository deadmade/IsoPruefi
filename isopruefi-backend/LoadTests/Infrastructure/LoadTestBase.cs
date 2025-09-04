using LoadTests.Seeder;
using Microsoft.Extensions.Configuration;

namespace LoadTests.Infrastructure;

/// <summary>
///     Base class for load tests using TestContainers and API factory
/// </summary>
public abstract class LoadTestBase
{
    /// <summary>
    ///     Configuration for the load test environment
    /// </summary>
    protected IConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    ///     Factory for creating REST API test infrastructure
    /// </summary>
    protected LoadTestRestAPIFactory ApiFactory { get; private set; } = null!;

    /// <summary>
    ///     Factory for creating MQTT test infrastructure
    /// </summary>
    protected LoadTestMqttFactory MqttFactory { get; private set; } = null!;

    /// <summary>
    ///     HTTP client for API testing
    /// </summary>
    protected HttpClient ApiClient { get; private set; } = null!;

    /// <summary>
    ///     Sets up the test environment with all required infrastructure
    /// </summary>
    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // Load configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            //.AddJsonFile("appsettings.loadtest.json")
            .AddEnvironmentVariables()
            .Build();

        // Initialize factory and containers
        ApiFactory = new LoadTestRestAPIFactory();
        await ApiFactory.InitializeAsync();

        await SensorSeeder.SeedTestDataAsync(ApiFactory.Services);
        await SensorSeeder.CheckSensorExistsAsync(ApiFactory.Services, 10);

        await InfluxSeeder.CreateIsoPrüfiDatabase(ApiFactory.Services);
        await InfluxSeeder.CheckDatabaseExists(ApiFactory.Services);

        MqttFactory = new LoadTestMqttFactory(ApiFactory.DatabaseConnectionString, ApiFactory.InfluxDbToken,
            ApiFactory.InfluxDbUrl);
        await MqttFactory.InitializeAsync();

        // Create HTTP client from factory
        ApiClient = ApiFactory.CreateClient();
    }

    /// <summary>
    ///     Cleans up the test infrastructure after all tests complete
    /// </summary>
    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        ApiClient?.Dispose();

        if (ApiFactory != null)
        {
            await ApiFactory.CleanupAsync();
            await MqttFactory.CleanupAsync();
            ApiFactory.Dispose();
        }

        Console.WriteLine("Load test infrastructure cleaned up");
    }


    /// <summary>
    ///     Get the API base URL from the factory
    /// </summary>
    protected string GetApiBaseUrl()
    {
        var baseAddress = ApiClient.BaseAddress?.ToString().TrimEnd('/');
        if (string.IsNullOrEmpty(baseAddress)) return "http://localhost";
        return baseAddress;
    }
}