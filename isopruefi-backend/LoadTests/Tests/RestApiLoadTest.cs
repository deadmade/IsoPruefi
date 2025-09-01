using NBomber.CSharp;
using NBomber.Http.CSharp;
using LoadTests.Infrastructure;

namespace LoadTests.Tests;

/// <summary>
/// Load tests for REST API endpoints using TestContainers and API factory
/// </summary>
[TestFixture]
public class RestApiLoadTest : LoadTestBase
{
    private AuthHelper? _authHelper;
    private string? _authToken;

    [OneTimeSetUp]
    public async Task RestApiSetup()
    {
        await GlobalSetup();
        
        // Setup authentication using the factory's API client
        _authHelper = new AuthHelper(GetApiBaseUrl());
        
        try 
        {
            _authToken = await _authHelper.GetAuthTokenAsync();
            Console.WriteLine("Authentication successful for REST API load test");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed, using test without auth: {ex.Message}");
            // Continue without auth for now - some endpoints might be public
        }
    }

    [OneTimeTearDown]
    public async Task RestApiTeardown()
    {
        _authHelper?.Dispose();
        await GlobalTeardown();
    }

    [Test]
    public async Task Test_1k_REST_API_Users_Temperature_Data_Requests()
    {
        var scenario = Scenario.Create("rest_api_1k_users", async context =>
        {
            // Generate random test data for each request
            var (startDate, endDate) = TestDataGenerator.GenerateRandomDateRange();
            var location = TestDataGenerator.GenerateRandomLocation();
            var isFahrenheit = Random.Shared.NextDouble() > 0.5;

            // Create the HTTP request using NBomber.Http and factory's ApiClient
            var request = Http.CreateRequest("GET", $"{GetApiBaseUrl()}/api/v1/TemperatureData/GetTemperature?start={Uri.EscapeDataString(startDate.ToString("O"))}&end={Uri.EscapeDataString(endDate.ToString("O"))}&place={Uri.EscapeDataString(location)}&isFahrenheit={isFahrenheit.ToString().ToLower()}")
                .WithHeader("Accept", "application/json");

            // Add authorization if available
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            var response = await Http.Send(ApiClient, request);
            return response;
        })
        .WithLoadSimulations(
            // Warm up
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Ramp up to 1k users over 1 minute
            Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            // Sustain 1k concurrent users
            Simulation.KeepConstant(copies: Settings.LoadTestScenarios.RestApiUserCount, during: TimeSpan.FromMinutes(Settings.LoadTestScenarios.TestDurationMinutes))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        Console.WriteLine($"REST API - Success Rate: {stats.AllOkCount}/{stats.AllRequestCount} ({stats.AllOkCount * 100.0 / stats.AllRequestCount:F1}%)");
        Console.WriteLine($"REST API - Requests/sec: {stats.AllOkCount / (Settings.LoadTestScenarios.TestDurationMinutes * 60.0):F1}");

        // Test assertions
        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(95), 
            "REST API success rate should be > 95%");
    }

    [Test]
    public async Task Test_REST_API_Authentication_Load()
    {
        var scenario = Scenario.Create("rest_api_auth_load", async context =>
        {
            // Test authentication endpoint load
            var request = Http.CreateRequest("POST", $"{GetApiBaseUrl()}/api/v1/Authentication/Login")
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Accept", "application/json")
                .WithBody(new StringContent("""
                {
                    "username": "loadtest@example.com",
                    "password": "LoadTest123!",
                    "rememberMe": false
                }
                """, System.Text.Encoding.UTF8, "application/json"));

            var response = await Http.Send(ApiClient, request);
            return response;
        })
        .WithLoadSimulations(
            // Test authentication under moderate load
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(2))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        Console.WriteLine($"Auth API - Success Rate: {stats.AllOkCount}/{stats.AllRequestCount} ({stats.AllOkCount * 100.0 / stats.AllRequestCount:F1}%)");
    }

    [Test]
    public async Task Test_REST_API_Mixed_Endpoints_Load()
    {
        var scenario = Scenario.Create("rest_api_mixed_endpoints", async context =>
        {
            var endpointChoice = Random.Shared.Next(4);
            
            var request = endpointChoice switch
            {
                0 => Http.CreateRequest("GET", $"{GetApiBaseUrl()}/api/v1/TemperatureData/GetTemperature?start={Uri.EscapeDataString(DateTime.UtcNow.AddHours(-1).ToString("O"))}&end={Uri.EscapeDataString(DateTime.UtcNow.ToString("O"))}&place={Uri.EscapeDataString(TestDataGenerator.GenerateRandomLocation())}"),
                    
                1 => Http.CreateRequest("GET", $"{GetApiBaseUrl()}/api/v1/Location"),
                
                2 => Http.CreateRequest("GET", $"{GetApiBaseUrl()}/api/v1/Topic"),
                
                _ => Http.CreateRequest("GET", $"{GetApiBaseUrl()}/api/v1/UserInfo")
            };

            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            var response = await Http.Send(ApiClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 200, during: TimeSpan.FromMinutes(3))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        Console.WriteLine($"Mixed API - Success Rate: {stats.AllOkCount}/{stats.AllRequestCount} ({stats.AllOkCount * 100.0 / stats.AllRequestCount:F1}%)");

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(90), 
            "Mixed endpoints success rate should be > 90%");
    }
}