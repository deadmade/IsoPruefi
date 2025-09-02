using System.Net.Http.Json;
using System.Text;
using LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Database.EntityFramework;

namespace LoadTests.Tests;

/// <summary>
/// Comprehensive REST API load tests simulating realistic usage scenarios
/// </summary>
[TestFixture]
public class RestApiLoadTest : LoadTestBase
{
    private AuthHelper? _authHelper;
    private string? _authToken;
    private List<string>? _testSensorNames;

    [OneTimeSetUp]
    public async Task RestApiLoadTestSetup()
    {
        _authHelper = new AuthHelper(GetApiBaseUrl());
        
        try
        {
            _authToken = await _authHelper.GetAuthTokenAsync();
            Console.WriteLine("Authentication successful for REST API load test");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
        }

        // Get existing sensor names from the database for realistic queries
        using var scope = ApiFactory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _testSensorNames = context.TopicSettings
            .Where(ts => ts.SensorName != null)
            .Select(ts => ts.SensorName!)
            .Take(20)
            .ToList();
    }

    [OneTimeTearDown]
    public async Task RestApiLoadTestTeardown()
    {
        _authHelper?.Dispose();
        await GlobalTeardown();
    }

    [Test]
    public void Test_Sensor_Data_Queries_Load()
    {
        var baseUrl = GetApiBaseUrl();
        
        var sensorDataScenario = Scenario.Create("sensor_data_queries", async context =>
        {
            if (_testSensorNames == null || !_testSensorNames.Any())
                return Response.Fail(400);

            var random = Random.Shared;
            var sensorName = _testSensorNames[random.Next(_testSensorNames.Count)];
            
            // Generate realistic date ranges for queries
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-random.Next(1, 30)); // Last 1-30 days
            
            var url = $"{baseUrl}/api/SensorData/GetSensorDataBySensorNameAndDateRange?" +
                     $"sensorName={Uri.EscapeDataString(sensorName)}&" +
                     $"startDate={startDate:yyyy-MM-dd}&" +
                     $"endDate={endDate:yyyy-MM-dd}";

            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(30, TimeSpan.FromMinutes(2)) // 30 concurrent users for 2 minutes
        );

        var stats = NBomberRunner
            .RegisterScenarios(sensorDataScenario)
            .Run();

        Console.WriteLine("=== SENSOR DATA QUERIES LOAD TEST RESULTS ===");
        Console.WriteLine($"Total Requests: {stats.AllRequestCount}");
        Console.WriteLine($"Success Rate: {stats.AllOkCount * 100.0 / stats.AllRequestCount:F1}%");
        Console.WriteLine($"Requests/sec: {stats.AllOkCount / 120.0:F1}");

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(95),
            "Sensor data queries should have > 95% success rate");
        Assert.That(stats.ScenarioStats[0].Ok.Latency, Is.LessThan(2000),
            "Average response time should be < 2000ms");
    }

    [Test]
    public void Test_Sensor_Management_Operations()
    {
        var baseUrl = GetApiBaseUrl();
        
        var sensorListScenario = Scenario.Create("list_sensors", async context =>
        {
            var url = $"{baseUrl}/api/Sensor/GetAllSensors";
            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(20, TimeSpan.FromMinutes(1))
        );

        var sensorDetailsScenario = Scenario.Create("sensor_details", async context =>
        {
            if (_testSensorNames == null || !_testSensorNames.Any())
                return Response.Fail(400);

            var random = Random.Shared;
            var sensorName = _testSensorNames[random.Next(_testSensorNames.Count)];
            
            var url = $"{baseUrl}/api/Sensor/GetSensorBySensorName?sensorName={Uri.EscapeDataString(sensorName)}";
            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(15, TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(sensorListScenario, sensorDetailsScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(95),
            "Sensor management operations should have > 95% success rate");
    }

    [Test]
    public void Test_Dashboard_Data_Load()
    {
        var baseUrl = GetApiBaseUrl();
        
        var dashboardScenario = Scenario.Create("dashboard_data", async context =>
        {
            // Simulate typical dashboard queries
            var random = Random.Shared;
            var queryType = random.Next(1, 4);
            
            string url = queryType switch
            {
                1 => $"{baseUrl}/api/SensorData/GetLatestSensorData",
                2 => $"{baseUrl}/api/SensorData/GetSensorDataSummary?days={random.Next(1, 7)}",
                3 => $"{baseUrl}/api/Sensor/GetSensorStatus",
                _ => $"{baseUrl}/api/Health"
            };

            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(50, TimeSpan.FromMinutes(1)) // High concurrency for dashboard
        );

        var stats = NBomberRunner
            .RegisterScenarios(dashboardScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(90),
            "Dashboard queries should handle high load with > 90% success rate");
    }

    [Test]
    public void Test_Mixed_Api_Usage_Patterns()
    {
        var baseUrl = GetApiBaseUrl();
        
        // Simulate read-heavy workload (typical for monitoring systems)
        var readHeavyScenario = Scenario.Create("read_heavy", async context =>
        {
            var random = Random.Shared;
            var operations = new[]
            {
                () => $"{baseUrl}/api/SensorData/GetLatestSensorData",
                () => $"{baseUrl}/api/Sensor/GetAllSensors",
                () => _testSensorNames != null && _testSensorNames.Any() 
                    ? $"{baseUrl}/api/SensorData/GetSensorDataBySensorNameAndDateRange?sensorName={Uri.EscapeDataString(_testSensorNames[random.Next(_testSensorNames.Count)])}&startDate={DateTime.UtcNow.AddHours(-24):yyyy-MM-dd}&endDate={DateTime.UtcNow:yyyy-MM-dd}"
                    : $"{baseUrl}/api/Health",
                () => $"{baseUrl}/api/Health"
            };

            var operation = operations[random.Next(operations.Length)];
            var url = operation();
            
            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            // Add some realistic delay between requests
            if (random.Next(1, 100) <= 20) // 20% of requests have user think time
            {
                await Task.Delay(random.Next(100, 2000));
            }

            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(40, TimeSpan.FromMinutes(2)) // 40 concurrent users
        );

        // Simulate periodic batch operations (like reports or exports)
        var batchOperationsScenario = Scenario.Create("batch_operations", async context =>
        {
            var random = Random.Shared;
            
            if (_testSensorNames == null || !_testSensorNames.Any())
                return Response.Fail();
            
            // Simulate larger data queries (like generating reports)
            var sensorName = _testSensorNames[random.Next(_testSensorNames.Count)];
            var daysBack = random.Next(30, 90); // Larger date ranges
            var startDate = DateTime.UtcNow.AddDays(-daysBack);
            var endDate = DateTime.UtcNow;
            
            var url = $"{baseUrl}/api/SensorData/GetSensorDataBySensorNameAndDateRange?" +
                     $"sensorName={Uri.EscapeDataString(sensorName)}&" +
                     $"startDate={startDate:yyyy-MM-dd}&" +
                     $"endDate={endDate:yyyy-MM-dd}";

            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(5, TimeSpan.FromMinutes(1)) // Lower concurrency but heavy operations
        );

        var stats = NBomberRunner
            .RegisterScenarios(readHeavyScenario, batchOperationsScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(90),
            "Mixed API usage should maintain > 90% success rate");
    }

    [Test]
    public void Test_Stress_Api_Endpoints()
    {
        var baseUrl = GetApiBaseUrl();
        
        var stressScenario = Scenario.Create("stress_test", async context =>
        {
            var random = Random.Shared;
            
            // Focus on most critical endpoints under stress
            var endpoints = new[]
            {
                $"{baseUrl}/api/Health",
                $"{baseUrl}/api/SensorData/GetLatestSensorData",
                $"{baseUrl}/api/Sensor/GetAllSensors"
            };

            var url = endpoints[random.Next(endpoints.Length)];
            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),  // Warm up
            Simulation.KeepConstant(50, TimeSpan.FromSeconds(30)),  // Ramp up
            Simulation.KeepConstant(100, TimeSpan.FromSeconds(60)), // Peak load
            Simulation.KeepConstant(50, TimeSpan.FromSeconds(30)),  // Ramp down
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30))   // Cool down
        );

        var stats = NBomberRunner
            .RegisterScenarios(stressScenario)
            .Run();

        Console.WriteLine("=== STRESS API ENDPOINTS TEST RESULTS ===");
        Console.WriteLine($"Total Requests Under Stress: {stats.AllRequestCount}");
        Console.WriteLine($"Success Rate: {stats.AllOkCount * 100.0 / stats.AllRequestCount:F1}%");
        Console.WriteLine($"Peak Requests/sec: {stats.AllOkCount / 180.0:F1}");


        // More lenient success rate for stress test
        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(85),
            "API should handle stress with > 85% success rate");

    }
}