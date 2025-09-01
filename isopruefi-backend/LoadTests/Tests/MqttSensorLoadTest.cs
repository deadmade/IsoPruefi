using System.Diagnostics;
using System.Text.Json;
using Database.Repository.InfluxRepo;
using LoadTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;
using NBomber.CSharp;
using MqttClient = NBomber.MQTT.MqttClient;

namespace LoadTests.Tests;

/// <summary>
///     Load tests for MQTT sensor data publishing using TestContainers
/// </summary>
[TestFixture]
public class MqttSensorLoadTest : LoadTestBase
{
    [OneTimeSetUp]
    public async Task TestSetup()
    {
        // Seed test sensors in database
        _testSensorNames = await LoadTestDataSeeder.SeedTestDataAsync(Factory.Services, Settings.SensorCount);

        // Start MQTT-Receiver-Worker process
        _mqttReceiverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project ../MQTT-Receiver-Worker/MQTT-Receiver-Worker.csproj",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        _mqttReceiverProcess.StartInfo.EnvironmentVariables["Influx__InfluxDBToken"] = Factory.InfluxDbToken;
        _mqttReceiverProcess.StartInfo.EnvironmentVariables["Mqtt__BrokerPort"] = "localhost";
        _mqttReceiverProcess.StartInfo.EnvironmentVariables["Mqtt__BrokerPort"] = Factory.MqttPort.ToString();
        _mqttReceiverProcess.Start();
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [OneTimeTearDown]
    public async Task TestCleanup()
    {
        // Stop MQTT-Receiver-Worker process
        if (_mqttReceiverProcess != null && !_mqttReceiverProcess.HasExited)
        {
            _mqttReceiverProcess.Kill();
            await _mqttReceiverProcess.WaitForExitAsync();
            _mqttReceiverProcess.Dispose();
        }

        await GlobalTeardown();
    }

    private List<Tuple<string,string>> _testSensorNames = new();
    private readonly Random rnd = Random.Shared;
    private Process? _mqttReceiverProcess;

    [Test]
    public async Task Test_MQTT_Receving()
    {
        var start = DateTime.UtcNow;

        var scenario = Scenario.Create("mqtt_receving", async ctx =>
            {
                using var mqttClient = new MqttClient(new MqttClientFactory().CreateMqttClient());

                var connect = await Step.Run(Guid.NewGuid().ToString(), ctx, async () =>
                {
                    var options = new MqttClientOptionsBuilder()
                        .WithTcpServer("localhost", Factory.MqttPort)
                        .Build();

                    return await mqttClient.Connect(options, CancellationToken.None);
                });

                foreach (var sensor in _testSensorNames)
                {
                    var publish = await Step.Run(Guid.NewGuid().ToString(), ctx, async () =>
                    {
                        var msg = new MqttApplicationMessageBuilder()
                            .WithTopic(sensor.Item1)
                            .WithPayload(GenerateTemperature())
                            .Build();

                        return await mqttClient.Publish(msg);
                    });
                }

                return Response.Ok();
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(3))
            .WithLoadSimulations(
                Simulation.KeepConstant(1, TimeSpan.FromSeconds(4))
            );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var successRate = stats.AllOkCount * 100.0 / stats.AllRequestCount;

        Assert.That(successRate, Is.GreaterThan(90),
            $"MQTT recovery publishing success rate should be > 90%, but was {successRate:F1}%");

        var end = DateTime.UtcNow;
        await VerifyInfluxDBData(start, end);
    }

    public string GenerateTemperature()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        double?[]? value = [Math.Round(rnd.NextDouble() * 100, 1)];

        var tempGen = new TempSensorReading
        {
            Timestamp = timestamp, Value = value, Sequence = rnd.Next()
        };
        var json = JsonSerializer.Serialize(tempGen);

        return json;
    }

    private async Task VerifyInfluxDBData(DateTime start, DateTime end)
    {
        // Get InfluxDB service from your Database project
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();

        foreach (var sensor in _testSensorNames)
        {
            var recordCount = 0;
            await foreach (var row in repo.GetSensorWeatherData(start, end, sensor.Item2)) recordCount++;

            var expectedCount = _testSensorNames.Count;

            Assert.That(recordCount, Is.GreaterThan(0),
                $"Expected {expectedCount} records in InfluxDB but found {recordCount}");
        }
    }
}