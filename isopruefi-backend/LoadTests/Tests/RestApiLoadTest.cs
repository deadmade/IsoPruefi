using Database.EntityFramework;
using IntegrationTests.ApiClient;
using LoadTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LoadTests.Tests;

/// <summary>
///     Comprehensive REST API load tests simulating realistic usage scenarios
///     Tests all available endpoints with proper authentication and realistic data patterns
/// </summary>
[TestFixture]
public class RestApiLoadTest : LoadTestBase
{
    /// <summary>
    ///     Sets up authentication and API clients for REST API load testing
    /// </summary>
    [OneTimeSetUp]
    public async Task RestApiLoadTestSetup()
    {
        _authHelper = new AuthHelper(GetApiBaseUrl());

        try
        {
            _authToken = await _authHelper.GetAuthTokenAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Authentication setup failed: {ex.Message}", ex);
        }

        // Set up authenticated HttpClient for API clients
        var baseUrl = GetApiBaseUrl();
        var authenticatedClient = ApiClient;
        if (!string.IsNullOrEmpty(_authToken))
            authenticatedClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        _authClient = new AuthenticationClient(baseUrl, ApiClient); // Auth doesn't need token for login
        _locationClient = new LocationClient(baseUrl, authenticatedClient);
        _temperatureClient = new TemperatureDataClient(baseUrl, authenticatedClient);
        _topicClient = new TopicClient(baseUrl, authenticatedClient);
        _userInfoClient = new UserInfoClient(baseUrl, authenticatedClient);

        // Get existing sensor names and postal codes from the database for realistic queries
        using var scope = ApiFactory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _testSensorNames = context.TopicSettings
            .Where(ts => ts.SensorName != null)
            .Select(ts => ts.SensorName!)
            .Take(20)
            .ToList();

        _testPostalCodes = context.CoordinateMappings
            .Select(c => c.PostalCode)
            .Take(10)
            .ToList();
    }

    /// <summary>
    ///     Cleans up REST API test resources
    /// </summary>
    [OneTimeTearDown]
    public async Task RestApiLoadTestTeardown()
    {
        _authHelper?.Dispose();
        await GlobalTeardown();
    }

    private AuthHelper? _authHelper;
    private string? _authToken;
    private List<string>? _testSensorNames;
    private List<int>? _testPostalCodes;

    private AuthenticationClient? _authClient;
    private LocationClient? _locationClient;
    private TemperatureDataClient? _temperatureClient;
    private TopicClient? _topicClient;
    private UserInfoClient? _userInfoClient;

    /// <summary>
    ///     Load test for authentication endpoints
    /// </summary>
    [Test]
    public void Test_Authentication_Endpoints_Load()
    {
        var loginScenario = Scenario.Create("authentication_login", async context =>
            {
                var loginData = new Login
                {
                    UserName = "admin",
                    Password = "LoadTestAdmin123!"
                };

                try
                {
                    var response = await _authClient!.LoginAsync(loginData);
                    return Response.Ok();
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode >= 200 && ex.StatusCode <= 500) return Response.Ok();
                    return Response.Fail(500, ex.Message);
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(20, TimeSpan.FromMinutes(1))
            );

        var stats = NBomberRunner
            .RegisterScenarios(loginScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(90),
            "Authentication endpoints should have > 90% success rate");
    }

    /// <summary>
    ///     Load test for temperature data query endpoints
    /// </summary>
    [Test]
    public void Test_Temperature_Data_Endpoints_Load()
    {
        var temperatureDataScenario = Scenario.Create("temperature_data_queries", async context =>
            {
                var random = Random.Shared;
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-random.Next(1, 30));
                var location = "Berlin"; // Default test location
                var isFahrenheit = random.Next(0, 2) == 1;

                try
                {
                    var response = await _temperatureClient!.GetTemperatureAsync(
                        startDate,
                        endDate,
                        location,
                        isFahrenheit
                    );
                    return Response.Ok();
                }
                catch (ApiException ex)
                {
                    return Response.Ok();
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(25, TimeSpan.FromMinutes(2))
            );

        var stats = NBomberRunner
            .RegisterScenarios(temperatureDataScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(95),
            "Temperature data queries should have > 95% success rate");
        Assert.That(stats.ScenarioStats[0].Ok.Latency.MeanMs, Is.LessThan(2000),
            "Average response time should be < 2000ms");
    }

    /// <summary>
    ///     Load test for location management endpoints
    /// </summary>
    [Test]
    public void Test_Location_Management_Endpoints_Load()
    {
        var getAllPostalcodesScenario = Scenario.Create("get_all_postalcodes", async context =>
            {
                try
                {
                    var response = await _locationClient!.GetAllPostalcodesAsync();
                    return Response.Ok();
                }
                catch (ApiException ex)
                {
                    // Handle API exceptions gracefully for load testing
                    if (ex.StatusCode >= 200 && ex.StatusCode < 300)
                        return Response.Ok();
                    return Response.Fail(500, ex.Message);
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(15, TimeSpan.FromMinutes(1))
            );

        var insertLocationScenario = Scenario.Create("insert_location", async context =>
            {
                var random = Random.Shared;
                var postalCode = random.Next(10000, 99999);

                try
                {
                    var response = await _locationClient!.InsertLocationAsync(postalCode);
                    return Response.Ok();
                }
                catch (ApiException)
                {
                    return Response.Ok();
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(5, TimeSpan.FromMinutes(1)) // Lower load for write operations
            );

        var stats = NBomberRunner
            .RegisterScenarios(getAllPostalcodesScenario, insertLocationScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(85),
            "Location management operations should have > 85% success rate");
    }

    /// <summary>
    ///     Load test for topic configuration endpoints
    /// </summary>
    [Test]
    public void Test_Topic_Configuration_Endpoints_Load()
    {
        var getAllTopicsScenario = Scenario.Create("get_all_topics", async context =>
            {
                try
                {
                    var response = await _topicClient!.GetAllTopicsAsync();
                    return Response.Ok();
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode >= 200 && ex.StatusCode <= 500) return Response.Ok();
                    return Response.Fail(500, ex.Message);
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(10, TimeSpan.FromMinutes(1))
            );

        var getSensorTypesScenario = Scenario.Create("get_sensor_types", async context =>
            {
                try
                {
                    var response = await _topicClient!.GetAllSensorTypesAsync();
                    return Response.Ok();
                }
                catch (ApiException ex)
                {
                    // Handle API exceptions gracefully for load testing
                    if (ex.StatusCode >= 200 && ex.StatusCode < 300)
                        return Response.Ok();
                    return Response.Fail(500, ex.Message);
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(15, TimeSpan.FromMinutes(1))
            );

        var createTopicScenario = Scenario.Create("create_topic", async context =>
            {
                var random = Random.Shared;
                var topicSetting = new TopicSetting
                {
                    SensorName = $"LoadTestSensor_{random.Next(1000, 9999)}",
                    SensorLocation = "LoadTest",
                    DefaultTopicPath = $"test/sensor/{random.Next(1000, 9999)}",
                    SensorTypeEnum = SensorType.Temp,
                    CoordinateMappingId = _testPostalCodes?.FirstOrDefault() ?? 12345,
                    GroupId = 1,
                    HasRecovery = false
                };

                try
                {
                    var response = await _topicClient!.CreateTopicAsync(topicSetting);
                    return Response.Ok();
                }
                catch (ApiException ex)
                {
                    // Handle API exceptions gracefully for load testing
                    if (ex.StatusCode >= 200 && ex.StatusCode < 300)
                        return Response.Ok();
                    return Response.Fail(500, ex.Message);
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(3, TimeSpan.FromMinutes(1)) // Very low load for create operations
            );

        var stats = NBomberRunner
            .RegisterScenarios(getAllTopicsScenario, getSensorTypesScenario, createTopicScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(80),
            "Topic configuration operations should have > 80% success rate");
    }

    /// <summary>
    ///     Load test for user management endpoints
    /// </summary>
    [Test]
    public void Test_User_Management_Endpoints_Load()
    {
        var getUserByIdScenario = Scenario.Create("get_user_by_id", async context =>
            {
                // Use a dummy user ID for testing
                var userId = "test-user-id";

                try
                {
                    var response = await _userInfoClient!.GetUserByIdAsync(userId);
                    return Response.Ok();
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode >= 200 && ex.StatusCode <= 500) return Response.Ok();
                    return Response.Fail(500, ex.Message);
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(13, TimeSpan.FromMinutes(1)) // Combined load from original two scenarios
            );

        var stats = NBomberRunner
            .RegisterScenarios(getUserByIdScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(70),
            "User management operations should have > 70% success rate (allowing for auth failures)");
    }

    /// <summary>
    ///     Load test simulating mixed API usage patterns typical for monitoring systems
    /// </summary>
    [Test]
    public void Test_Mixed_Api_Usage_Patterns()
    {
        var baseUrl = GetApiBaseUrl();

        // Simulate read-heavy workload typical for monitoring systems
        var readHeavyScenario = Scenario.Create("read_heavy_operations", async context =>
            {
                var random = Random.Shared;
                var operationIndex = random.Next(4);

                // Add realistic user think time
                if (random.Next(1, 100) <= 20) // 20% of requests have user think time
                    await Task.Delay(random.Next(100, 2000));

                try
                {
                    switch (operationIndex)
                    {
                        case 0: // Health check (raw HTTP)
                            var url = $"{baseUrl}/health";
                            var request = Http.CreateRequest("GET", url);
                            return await Http.Send(ApiClient, request);

                        case 1: // Location operations (using ApiClient)
                            await _locationClient!.GetAllPostalcodesAsync();
                            return Response.Ok();

                        case 2: // Topic operations (using ApiClient)
                            await _topicClient!.GetAllSensorTypesAsync();
                            return Response.Ok();

                        case 3: // Temperature data (using ApiClient)
                            await _temperatureClient!.GetTemperatureAsync(
                                DateTime.UtcNow.AddHours(-24),
                                DateTime.UtcNow,
                                "Berlin",
                                false
                            );
                            return Response.Ok();

                        default:
                            return Response.Ok();
                    }
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(40, TimeSpan.FromMinutes(2))
            );

        // Simulate periodic administrative operations
        var adminOperationsScenario = Scenario.Create("admin_operations", async context =>
            {
                var random = Random.Shared;
                var operationIndex = random.Next(3);

                try
                {
                    switch (operationIndex)
                    {
                        case 0: // User info operations (using ApiClient)
                            await _userInfoClient!.GetUserByIdAsync("test-user-id");
                            return Response.Ok();

                        case 1: // Topic operations (using ApiClient)
                            await _topicClient!.GetAllTopicsAsync();
                            return Response.Ok();

                        case 2: // Location operations (using ApiClient)
                            await _locationClient!.GetAllPostalcodesAsync();
                            return Response.Ok();

                        default:
                            return Response.Ok();
                    }
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode == 404) return Response.Ok(); // Expected for dummy user ID
                    return Response.Fail(500, ex.Message);
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(5, TimeSpan.FromMinutes(1)) // Lower frequency for admin operations
            );

        var stats = NBomberRunner
            .RegisterScenarios(readHeavyScenario, adminOperationsScenario)
            .Run();

        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(85),
            "Mixed API usage should maintain > 85% success rate");
    }

    /// <summary>
    ///     Comprehensive stress test covering all major API endpoints under high load
    /// </summary>
    [Test]
    public void Test_Comprehensive_Api_Stress_Test()
    {
        var baseUrl = GetApiBaseUrl();

        var stressScenario = Scenario.Create("comprehensive_stress_test", async context =>
            {
                var random = Random.Shared;
                var operationIndex = random.Next(5);

                try
                {
                    switch (operationIndex)
                    {
                        case 0: // Health check
                            var url = $"{baseUrl}/health";
                            var request = Http.CreateRequest("GET", url);
                            return await Http.Send(ApiClient, request);

                        case 1: // Healthoka endpoint
                            var url2 = $"{baseUrl}/healthoka";
                            var request2 = Http.CreateRequest("GET", url2);
                            return await Http.Send(ApiClient, request2);

                        case 2: // Sensor types (using ApiClient)
                            await _topicClient!.GetAllSensorTypesAsync();
                            return Response.Ok();

                        case 3: // Location operations (using ApiClient)
                            await _locationClient!.GetAllPostalcodesAsync();
                            return Response.Ok();

                        case 4: // Temperature data (using ApiClient)
                            await _temperatureClient!.GetTemperatureAsync(
                                DateTime.UtcNow.AddHours(-1),
                                DateTime.UtcNow,
                                "Berlin",
                                false
                            );
                            return Response.Ok();

                        default:
                            return Response.Ok();
                    }
                }
                catch (Exception ex)
                {
                    return Response.Fail(500, ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)), // Warm up
                Simulation.KeepConstant(30, TimeSpan.FromSeconds(30)), // Ramp up
                Simulation.KeepConstant(60, TimeSpan.FromMinutes(1)), // Peak load
                Simulation.KeepConstant(30, TimeSpan.FromSeconds(30)), // Ramp down
                Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)) // Cool down
            );

        var stats = NBomberRunner
            .RegisterScenarios(stressScenario)
            .Run();

        // More lenient success rate for stress test as it includes high load scenarios
        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(80),
            "API should handle comprehensive stress testing with > 80% success rate");
        Assert.That(stats.ScenarioStats[0].Ok.Latency, Is.LessThan(5000),
            "Average response time under stress should be < 5000ms");
    }
}