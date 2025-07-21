using Database.Repository.SettingsRepository;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTT_Receiver_Worker.MQTT;

public class Receiver
{
    private ISettingsRepo _settingsRepo;
    private Connection _connection;

    public Receiver(ISettingsRepo settingsRepo, Connection connection)
    {
        _settingsRepo = settingsRepo;
        _connection = connection;
    }

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