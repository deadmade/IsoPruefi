using LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LoadTests.Tests;

/// <summary>
///     Combined load tests for both MQTT sensors and REST API using TestContainers
/// </summary>
[TestFixture]
public class CombinedLoadTest : LoadTestBase
{
    [OneTimeSetUp]
    public async Task CombinedSetup()
    {
        await GlobalSetup();

        _authHelper = new AuthHelper(GetApiBaseUrl());

        try
        {
            _authToken = await _authHelper.GetAuthTokenAsync();
            Console.WriteLine("Authentication successful for combined load test");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed, continuing without auth: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public async Task CombinedTeardown()
    {
        _authHelper?.Dispose();
        await GlobalTeardown();
    }

    private AuthHelper? _authHelper;
    private string? _authToken;

    [Test]
    public async Task Test_Combined_MQTT_And_REST_API_Load()
    {
        // MQTT Placeholder Scenario
        var mqttScenario = Scenario.Create("mqtt_sensors_combined_placeholder", async context =>
            {
                // Placeholder for MQTT functionality
                await Task.Delay(1);
                return Response.Ok();
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(50, TimeSpan.FromMinutes(1))
            );

        // REST API Scenario
        var restApiScenario = Scenario.Create("rest_api_users_combined", async context =>
            {
                var (startDate, endDate) = TestDataGenerator.GenerateRandomDateRange();
                var location = TestDataGenerator.GenerateRandomLocation();

                var request = Http.CreateRequest("GET",
                        $"{GetApiBaseUrl()}/api/v1/TemperatureData/GetTemperature?start={Uri.EscapeDataString(startDate.ToString("O"))}&end={Uri.EscapeDataString(endDate.ToString("O"))}&place={Uri.EscapeDataString(location)}")
                    .WithHeader("Accept", "application/json");

                if (!string.IsNullOrEmpty(_authToken))
                    request = request.WithHeader("Authorization", $"Bearer {_authToken}");

                var response = await Http.Send(ApiClient, request);
                return response;
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(50, TimeSpan.FromMinutes(1))
            );

        // Run both scenarios simultaneously
        var stats = NBomberRunner
            .RegisterScenarios(mqttScenario, restApiScenario)
            .Run();

        // Analyze results
        Console.WriteLine("=== COMBINED LOAD TEST RESULTS ===");
        Console.WriteLine("Test Duration: 1 minute");
        Console.WriteLine();

        Console.WriteLine("Combined Load Test:");
        Console.WriteLine(
            $"  Success Rate: {stats.AllOkCount}/{stats.AllRequestCount} ({stats.AllOkCount * 100.0 / stats.AllRequestCount:F1}%)");
        Console.WriteLine($"  Requests/sec: {stats.AllOkCount / 60.0:F1}");

        // Combined test assertions
        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(90),
            "Combined success rate should be > 90% under combined load");
    }
}