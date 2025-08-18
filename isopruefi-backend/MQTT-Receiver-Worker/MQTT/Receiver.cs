using Database.Repository.SettingsRepo;
using MQTT_Receiver_Worker.MQTT.Interfaces;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTT_Receiver_Worker.MQTT;

/// <summary>
/// Handles MQTT topic subscription and message receiving functionality.
/// This class is responsible for subscribing to configured topics from the settings repository
/// and managing the connection to the MQTT broker.
/// </summary>
public class Receiver : IReceiver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly ILogger<Receiver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Receiver"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="connection">Connection manager for the MQTT client.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public Receiver(IServiceProvider serviceProvider, IConnection connection, ILogger<Receiver> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribes to configured MQTT topics using shared subscriptions.
    /// Retrieves topic settings from the repository and establishes subscriptions
    /// </summary>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    public async Task SubscribeToTopics()
    {
        _logger.LogInformation("Starting subscription to MQTT topics");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepo>();

            var topics = await settingsRepo.GetTopicSettingsAsync();
            var mqttClient = await _connection.GetConnectionAsync();

            if (mqttClient == null)
            {
                _logger.LogError("Cannot subscribe to topics: MQTT client is not connected");
                throw new InvalidOperationException("MQTT client is not connected");
            }

            _logger.LogDebug("MQTT connection established successfully");

            foreach (var topic in topics)
            {
#if DEBUG
                var groupName = "cute-temp-dev-group2";
#else
                var groupName = "cute-temp-group2";
#endif

                var sharedTopic =
                    $"$share/{groupName}/{topic.DefaultTopicPath}/{topic.GroupId}/{topic.SensorType}/{topic.SensorName}";

                if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
                {
                    sharedTopic = sharedTopic + "_Dev";
                }

                await SubscribeToTopic(sharedTopic, mqttClient, topic.HasRecovery);
            }

            _logger.LogInformation("Successfully subscribed to all {TopicCount} topics", topics.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to MQTT topics");
            throw;
        }
    }

    private async Task SubscribeToTopic(string topic, IMqttClient mqttClient, bool hasRecovery)
    {
        try
        {
            _logger.LogInformation("Subscribing to topic: {Topic}", topic);

            var filter = new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await mqttClient.SubscribeAsync(filter, CancellationToken.None);

            if (hasRecovery)
            {
                filter = new MqttTopicFilterBuilder()
                    .WithTopic(topic + "/recovered")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await mqttClient.SubscribeAsync(filter, CancellationToken.None);
                _logger.LogInformation("Successfully subscribed to topic and recovery topic: {Topic}", topic);
            }
            else
            {
                _logger.LogInformation("Successfully subscribed to topic: {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic: {Topic}", topic);
            throw;
        }
    }
}