using MQTTnet;
using MQTTnet.Formatter;

namespace MQTT_Receiver_Worker.MQTT;

public class Connection
{
    public static async Task<IMqttClient> GetConnection()
    {
        string broker ="aicon.dhbw-heidenheim.de";
        int port = 1883;
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
            //.WithCredentials(username, password) // Set username and password
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
        
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
           var message = e.ApplicationMessage.ConvertPayloadToString();
           Console.WriteLine(message);

            return Task.CompletedTask;
        };

        var response = await mqttClient.ConnectAsync(options, CancellationToken.None);
        return mqttClient;
    }
    
}