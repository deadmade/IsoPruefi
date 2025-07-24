using Database.EntityFramework.Models;
using Database.Repository.SettingsRepository;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTT_Receiver_Worker.MQTT;

/// <summary>
/// Handles MQTT topic subscription and message receiving functionality.
/// This class is responsible for subscribing to configured topics from the settings repository
/// and managing the connection to the MQTT broker.
/// </summary>
public class Receiver
{

    private readonly ISettingsRepo _settingsRepo;
    private readonly Connection _connection;
    private readonly ILogger<Receiver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Receiver"/> class.
    /// </summary>
    /// <param name="settingsRepo">Repository for retrieving topic settings.</param>
    /// <param name="connection">Connection manager for the MQTT client.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public Receiver(ISettingsRepo settingsRepo, Connection connection, ILogger<Receiver> logger)
    {
        _settingsRepo = settingsRepo;
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Subscribes to configured MQTT topics using shared subscriptions.
    /// Retrieves topic settings from the repository and establishes subscriptions
    /// </summary>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    public async Task SubscribeToTopics()
    {
        _logger.LogInformation("Starting subscription to MQTT topics");
        var topics = _settingsRepo.GetTopicSettingsAsync();

        var mqttClient = await  _connection.GetConnection();
        _logger.LogDebug("MQTT connection established successfully");

        foreach (var topic in await topics)
        {
            var groupName = "cute-temp-group2";
            var sharedTopic =
                $"$share/{groupName}/{topic.DefaultTopicPath}/{topic.GroupId}/{topic.SensorType}/{topic.SensorName}";

            SubscribeToTopic(sharedTopic, mqttClient);
        }
    }

    private void SubscribeToTopic(string topic, IMqttClient mqttClient)
    {
        _logger.LogInformation("Subscribing to topic: {Topic}", topic);

        var filter = new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        mqttClient.SubscribeAsync(filter, CancellationToken.None).Wait();
        _logger.LogInformation("Successfully subscribed to topic: {Topic}", topic);
    }
}