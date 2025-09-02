using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoadTests.Infrastructure;

/// <summary>
///     Base class for load tests using TestContainers and API factory
/// </summary>
public abstract class LoadTestBase
{
    protected IConfiguration Configuration { get; private set; } = null!;
    protected LoadTestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient ApiClient { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // Load configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.loadtest.json")
            .AddEnvironmentVariables()
            .Build();

        // Initialize factory and containers
        Factory = new LoadTestWebApplicationFactory();
        await Factory.InitializeAsync();

        // Create HTTP client from factory
        ApiClient = Factory.CreateClient();

        Console.WriteLine("Load test infrastructure initialized successfully");
        Console.WriteLine(
            $"API Base URL: {Factory.Services.GetRequiredService<IConfiguration>()["BaseUrl"] ?? "http://localhost"}");
        Console.WriteLine($"Database: {Factory.DatabaseConnectionString}");
        Console.WriteLine($"InfluxDB: {Factory.InfluxDbUrl} (Token: {Factory.InfluxDbToken})");
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        ApiClient?.Dispose();

        if (Factory != null)
        {
            await Factory.CleanupAsync();
            Factory.Dispose();
        }

        Console.WriteLine("Load test infrastructure cleaned up");
    }


    /// <summary>
    ///     Get the API base URL from the factory
    /// </summary>
    protected string GetApiBaseUrl()
    {
        var baseAddress = ApiClient.BaseAddress?.ToString().TrimEnd('/');
        if (string.IsNullOrEmpty(baseAddress))
        {
            return "http://localhost";
        }
        return baseAddress;
    }
}