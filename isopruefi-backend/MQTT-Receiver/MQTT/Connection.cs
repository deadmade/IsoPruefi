using MQTTnet;

namespace MQTT_Receiver.MQTT;

public class Connection
{
    public static Task<MqttClientConnectResult> GetConnection()
    {
        string broker ="aicon.dhbw-heidenheim.de";
        int port = 8883;
        string clientId = Guid.NewGuid().ToString();
        string username = "schueleinm.tin23";
        string password = "geheim";

        // Create a MQTT client factory
        var factory = new MqttClientFactory();

        // Create a MQTT client instance
        var mqttClient = factory.CreateMqttClient();

        // Create MQTT client options
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port) // MQTT broker address and port
            .WithCredentials(username, password) // Set username and password
            .WithClientId(clientId)
            .WithCleanSession()
            .Build();

        return  mqttClient.ConnectAsync(options);
    }
}