using System.Text;
using System.Text.Json;
using LoadTests.Infrastructure;
using LoadTests.MQTT;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;


namespace LoadTests.Tests;

/// <summary>
/// Combined load tests that simulate realistic scenarios with both MQTT sensor traffic and REST API usage
/// </summary>
[TestFixture]
public class IntegratedLoadTest : LoadTestBase
{
    private AuthHelper? _authHelper;
    private string? _authToken;
    private List<Tuple<string, string>>? _testSensors;
    private IMqttClient? _mqttClient;

    [OneTimeSetUp]
    public async Task IntegratedLoadTestSetup()
    {
        await GlobalSetup();
        
        _authHelper = new AuthHelper(GetApiBaseUrl());
        
        try
        {
            _authToken = await _authHelper.GetAuthTokenAsync();
            Console.WriteLine("Authentication successful for integrated load test");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
        }

        // Setup test sensors
        _testSensors = await LoadTestDataSeeder.SeedTestDataAsync(Factory.Services, 30);
        Console.WriteLine($"Created {_testSensors.Count} test sensors for integrated testing");

        // Setup MQTT client
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", Factory.MqttPort)
            .WithClientId($"IntegratedTestPublisher_{Guid.NewGuid()}")
            .Build();
            
        await _mqttClient.ConnectAsync(options);
        Console.WriteLine("MQTT publisher client connected for integrated test");
    }

    [OneTimeTearDown]
    public async Task IntegratedLoadTestTeardown()
    {
        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.DisconnectAsync();
        }
        _mqttClient?.Dispose();
        
        if (_testSensors != null)
        {
            await LoadTestDataSeeder.CleanupTestDataAsync(Factory.Services);
        }
        
        _authHelper?.Dispose();
        await GlobalTeardown();
    }

    [Test]
    public void Test_Realistic_IoT_System_Load()
    {
        var baseUrl = GetApiBaseUrl();
        
        // Scenario 1: Continuous sensor data publishing
        var sensorPublishingScenario = Scenario.Create("sensor_publishing", async context =>
        {
            if (_testSensors == null || !_testSensors.Any())
                return Response.Fail(400);

            var random = Random.Shared;
            var sensorData = _testSensors[random.Next(_testSensors.Count)];
            var topic = sensorData.Item1;

            var reading = new TempSensorReading
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Value = new double?[] { random.NextDouble() * 40.0 - 10.0 },
                Sequence = random.Next(1, 10000)
            };

            var payload = JsonSerializer.Serialize(reading);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _mqttClient!.PublishAsync(message);
                
                // Add realistic delays between sensor readings
                await Task.Delay(random.Next(5000, 15000)); // 5-15 second intervals
                return Response.Ok();
            }
            catch (Exception ex)
            {
                return Response.Fail(400);
            }
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(20, TimeSpan.FromMinutes(3)) // 20 sensors publishing for 3 minutes
        );

        // Scenario 2: Dashboard users monitoring data
        var dashboardMonitoringScenario = Scenario.Create("dashboard_monitoring", async context =>
        {
            var random = Random.Shared;
            var endpoints = new[]
            {
                $"{baseUrl}/api/SensorData/GetLatestSensorData",
                $"{baseUrl}/api/Sensor/GetAllSensors",
                $"{baseUrl}/api/Health"
            };

            var url = endpoints[random.Next(endpoints.Length)];
            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            // Simulate user browsing behavior
            await Task.Delay(random.Next(2000, 10000)); // 2-10 seconds between requests
            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(10, TimeSpan.FromMinutes(3)) // 10 dashboard users
        );

        // Scenario 3: Historical data queries
        var historicalDataScenario = Scenario.Create("historical_queries", async context =>
        {
            if (_testSensors == null || !_testSensors.Any())
                return Response.Fail(400);

            var random = Random.Shared;
            var sensorData = _testSensors[random.Next(_testSensors.Count)];
            var sensorName = sensorData.Item2;
            
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-random.Next(1, 7));
            
            var url = $"{baseUrl}/api/SensorData/GetSensorDataBySensorNameAndDateRange?" +
                     $"sensorName={Uri.EscapeDataString(sensorName)}&" +
                     $"startDate={startDate:yyyy-MM-dd}&" +
                     $"endDate={endDate:yyyy-MM-dd}";

            var request = Http.CreateRequest("GET", url);
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request = request.WithHeader("Authorization", $"Bearer {_authToken}");
            }

            // Historical queries are less frequent
            await Task.Delay(random.Next(15000, 60000)); // 15-60 seconds between queries
            return await Http.Send(ApiClient, request);
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(5, TimeSpan.FromMinutes(3)) // 5 users doing historical queries
        );

        var stats = NBomberRunner
            .RegisterScenarios(sensorPublishingScenario, dashboardMonitoringScenario, historicalDataScenario)
            .Run();
        

        // Validate system performance under realistic load
        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(90),
            "Integrated system should maintain > 90% success rate");
    }

    [Test]
    public void Test_Peak_Traffic_Simulation()
    {
        var baseUrl = GetApiBaseUrl();
        
        // Scenario: Simulating peak hours with higher sensor activity and user traffic
        var peakSensorTrafficScenario = Scenario.Create("peak_sensor_traffic", async context =>
        {
            if (_testSensors == null || !_testSensors.Any())
                return Response.Fail(400);

            var random = Random.Shared;
            var sensorData = _testSensors[random.Next(_testSensors.Count)];
            var topic = sensorData.Item1;

            var reading = new TempSensorReading
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Value = new double?[] { random.NextDouble() * 40.0 - 10.0 },
                Sequence = random.Next(1, 10000)
            };

            var payload = JsonSerializer.Serialize(reading);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _mqttClient!.PublishAsync(message);
                return Response.Ok();
            }
            catch (Exception ex)
            {
                return Response.Fail(400);
            }
        })
        .WithLoadSimulations(
            // Simulate peak hours traffic pattern
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),  // Normal
            Simulation.KeepConstant(30, TimeSpan.FromSeconds(30)),  // Building up
            Simulation.KeepConstant(50, TimeSpan.FromSeconds(60)),  // Peak
            Simulation.KeepConstant(30, TimeSpan.FromSeconds(30)),  // Reducing
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30))   // Back to normal
        );

        var peakApiTrafficScenario = Scenario.Create("peak_api_traffic", async context =>
        {
            var random = Random.Shared;
            var endpoints = new[]
            {
                $"{baseUrl}/api/SensorData/GetLatestSensorData",
                $"{baseUrl}/api/Sensor/GetAllSensors",
                $"{baseUrl}/api/Health"
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
            // Simulate peak hours API usage
            Simulation.KeepConstant(5, TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(15, TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(25, TimeSpan.FromSeconds(60)),
            Simulation.KeepConstant(15, TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(5, TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(peakSensorTrafficScenario, peakApiTrafficScenario)
            .Run();
        

        // Validate system handles peak traffic reasonably
        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(85),
            "System should handle peak traffic with > 85% success rate");
    }

    [Test]
    public void Test_System_Recovery_After_Spike()
    {
        var baseUrl = GetApiBaseUrl();
        
        // Test system behavior during and after traffic spikes
        var spikeRecoveryScenario = Scenario.Create("spike_recovery", async context =>
        {
            if (_testSensors == null || !_testSensors.Any())
                return Response.Fail(400);

            var random = Random.Shared;
            
            // Alternate between MQTT publishing and API calls
            if (random.Next(1, 3) == 1)
            {
                // MQTT publish
                var sensorData = _testSensors[random.Next(_testSensors.Count)];
                var topic = sensorData.Item1;

                var reading = new TempSensorReading
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Value = new double?[] { random.NextDouble() * 40.0 - 10.0 },
                    Sequence = random.Next(1, 10000)
                };

                var payload = JsonSerializer.Serialize(reading);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                try
                {
                    await _mqttClient!.PublishAsync(message);
                    return Response.Ok();
                }
                catch (Exception ex)
                {
                    return Response.Fail();
                }
            }
            else
            {
                // API call
                var url = $"{baseUrl}/api/Health";
                var request = Http.CreateRequest("GET", url);
                
                if (!string.IsNullOrEmpty(_authToken))
                {
                    request = request.WithHeader("Authorization", $"Bearer {_authToken}");
                }

                var response = await Http.Send(ApiClient, request);
                return response.IsError ? Response.Fail() : Response.Ok();
            }
        })
        .WithLoadSimulations(
            // Simulate normal -> spike -> recovery pattern
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),   // Normal load
            Simulation.KeepConstant(5, TimeSpan.FromSeconds(45)),   // Recovery period
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30))   // Back to normal
        );

        var stats = NBomberRunner
            .RegisterScenarios(spikeRecoveryScenario)
            .Run();

        Console.WriteLine("=== SYSTEM RECOVERY AFTER SPIKE TEST RESULTS ===");
        Console.WriteLine($"Total Spike/Recovery Operations: {stats.AllRequestCount}");
        Console.WriteLine($"Recovery Success Rate: {stats.AllOkCount * 100.0 / stats.AllRequestCount:F1}%");

        // Validate system can recover from spikes
        Assert.That(stats.AllOkCount * 100.0 / stats.AllRequestCount, Is.GreaterThan(80),
            "System should recover from spikes with > 80% overall success rate");
        Assert.That(stats.ScenarioStats[0].Ok.Latency, Is.LessThan(5000),
            "95% of requests should complete within 5 seconds even during spikes");
    }
}