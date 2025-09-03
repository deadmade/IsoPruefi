using System.Text.Json;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using LoadTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MQTT_Receiver_Worker.MQTT.Interfaces;
using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;
using NBomber.CSharp;
using MqttClient = NBomber.MQTT.MqttClient;
using TopicSetting = Database.EntityFramework.Models.TopicSetting;

namespace LoadTests.Tests;

/// <summary>
///     Load tests for MQTT sensor data publishing using TestContainers
/// </summary>
[TestFixture]
public class MqttSensorLoadTest : LoadTestBase
{
    /// <summary>
    ///     Sets up MQTT test environment and verifies connectivity
    /// </summary>
    [OneTimeSetUp]
    public async Task TestSetup()
    {
        using var scope = MqttFactory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISettingsRepo>();

        _topicSettings = await repo.GetTopicSettingsAsync();

        await Task.Delay(TimeSpan.FromSeconds(15));

        var connected = false;
        var subscribed = false;

        do
        {
            var health = scope.ServiceProvider.GetRequiredService<IConnection>();
            connected = health.IsConnected;
        } while (!connected);

        do
        {
            var health = scope.ServiceProvider.GetRequiredService<IConnection>();
            subscribed = health.IsSubscribed;
        } while (!subscribed);
    }

    private List<TopicSetting>? _topicSettings;
    private readonly Random _rnd = Random.Shared;

    /// <summary>
    ///     Load test for MQTT sensor data reception and processing
    /// </summary>
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
                        .WithTcpServer("localhost", MqttFactory.MqttPort)
                        .Build();

                    return await mqttClient.Connect(options, CancellationToken.None);
                });

                foreach (var sensor in _topicSettings!)
                {
                    var publish = await Step.Run(Guid.NewGuid().ToString(), ctx, async () =>
                    {
                        var msg = new MqttApplicationMessageBuilder()
                            .WithTopic(GenerateTopic(sensor))
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

    /// <summary>
    ///     Generates a properly formatted MQTT topic for the given topic setting
    /// </summary>
    /// <param name="topic">Topic setting configuration</param>
    /// <returns>Formatted MQTT topic string</returns>
    private string GenerateTopic(TopicSetting topic)
    {
        var topicString = $"{topic.DefaultTopicPath}/{topic.GroupId}/{topic.SensorTypeEnum}/{topic.SensorName}";

        return topicString;
    }

    /// <summary>
    ///     Generates a JSON payload with random temperature data
    /// </summary>
    /// <returns>JSON string containing temperature sensor reading</returns>
    public string GenerateTemperature()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        double?[]? value = [Math.Round(_rnd.NextDouble() * 100, 1)];

        var tempGen = new TempSensorReading
        {
            Timestamp = timestamp, Value = value, Sequence = _rnd.Next()
        };
        var json = JsonSerializer.Serialize(tempGen);

        return json;
    }

    /// <summary>
    ///     Verifies that MQTT data was successfully written to InfluxDB
    /// </summary>
    /// <param name="start">Start time for data verification</param>
    /// <param name="end">End time for data verification</param>
    private async Task VerifyInfluxDBData(DateTime start, DateTime end)
    {
        // Get InfluxDB service from your Database project
        using var scope = MqttFactory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();

        foreach (var sensor in _topicSettings!.OrderBy(x => x.SensorName))
        {
            var recordCount = 0;
            await foreach (var row in repo.GetSensorWeatherData(start, end, sensor.SensorName!)) recordCount++;

            Assert.That(recordCount, Is.GreaterThan(0),
                $"Expected more than 0 records in InfluxDB but found {recordCount} - Sensor: {sensor.SensorName}");
        }
    }
}