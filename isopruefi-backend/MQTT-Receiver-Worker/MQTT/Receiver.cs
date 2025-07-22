using Database.Repository.SettingsRepository;
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
    /// <summary>
    /// Repository for accessing MQTT topic configuration settings.
    /// </summary>
    private ISettingsRepo _settingsRepo;

    /// <summary>
    /// Connection manager for the MQTT client.
    /// </summary>
    private Connection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="Receiver"/> class.
    /// </summary>
    /// <param name="settingsRepo">Repository for retrieving topic settings.</param>
    /// <param name="connection">Connection manager for the MQTT client.</param>
    public Receiver(ISettingsRepo settingsRepo, Connection connection)
    {
        _settingsRepo = settingsRepo;
        _connection = connection;
    }

    /// <summary>
    /// Subscribes to configured MQTT topics using shared subscriptions.
    /// Retrieves topic settings from the repository and establishes subscriptions
    /// with QoS level "At Least Once".
    /// </summary>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    public async Task SubscribeToTopic()
    {
        var topics = _settingsRepo.GetTopicSettingsAsync();

        var mqttClient = await _connection.GetConnection();

        foreach (var topic in await topics)
        {
            var groupName = "cute-temp-group2";
            var sharedTopic =
                $"$share/{groupName}/{topic.DefaultTopicPath}/{topic.GroupId}/{topic.SensorType}/{topic.SensorName}";

            var filter = new MqttTopicFilterBuilder()
                .WithTopic(sharedTopic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await mqttClient.SubscribeAsync(filter, CancellationToken.None);
        }
    }
}