using Database.Repository.SettingsRepository;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTT_Receiver_Worker.MQTT;

public class Receiver
{
    private ISettingsRepo _settingsRepo;
    
    public Receiver(ISettingsRepo settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }
    
    public async Task Subscribe()
    {
        using (var mqttClient = await Connection.GetConnection())
        {
            string groupName = "cute-temp-group2";
            string topicFilter = "dhbw/ai/si2023/2/temperature/#";
            string sharedTopic = $"$share/{groupName}/{topicFilter}";
            
            var filter = new MqttTopicFilterBuilder()
                    .WithTopic(sharedTopic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

            await mqttClient.SubscribeAsync(filter, CancellationToken.None);

            Console.WriteLine("MQTT client subscribed to topic.");

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }
    }
}