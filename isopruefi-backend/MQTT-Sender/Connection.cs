using MQTTnet;
using MQTTnet.Formatter;

namespace MQTT_Sender;

public static class Connection
{
    public static async Task<IMqttClient> GetConnection()
    {
        var broker = "aicon.dhbw-heidenheim.de";
        var port = 1883;
        var clientId = Guid.NewGuid().ToString();
        var username = "schueleinm.tin23";
        var password = "geheim";

        // Create a MQTT client factory
        var factory = new MqttClientFactory();

        // Create a MQTT client instance
        var mqttClient = factory.CreateMqttClient();

        // Create MQTT client options
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port) // MQTT broker address and port
            .WithCredentials(username, password) // Set username and password
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .Build();

        mqttClient.DisconnectedAsync += async e =>
        {
            Console.WriteLine("Disconnected from MQTT broker. Attempting to reconnect...");
            try
            {
                await mqttClient.ConnectAsync(options, CancellationToken.None);
                Console.WriteLine("Reconnected successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reconnection failed: {ex.Message}");
            }
        };

        var response = await mqttClient.ConnectAsync(options, CancellationToken.None);
        return mqttClient;
    }
}