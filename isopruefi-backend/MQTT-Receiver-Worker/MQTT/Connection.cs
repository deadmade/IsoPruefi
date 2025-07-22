using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepository;
using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;
using MQTTnet.Formatter;

namespace MQTT_Receiver_Worker.MQTT;

/// <summary>
/// Handles MQTT broker connection and message processing.
/// Establishes a connection to the MQTT broker, subscribes to topics,
/// and processes incoming temperature sensor messages.
/// </summary>
public class Connection
{
    /// <summary>
    /// Repository for writing sensor data to InfluxDB
    /// </summary>
    IInfluxRepo _influxRepo;

    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="influxRepo">Repository for writing sensor data to InfluxDB</param>
    public Connection(IInfluxRepo influxRepo)
    {
        _influxRepo = influxRepo;
    }

    /// <summary>
    /// Establishes a connection to the MQTT broker and configures message handlers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the connected MQTT client.</returns>
    public async Task<IMqttClient> GetConnection()
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
            var topic = e.ApplicationMessage.Topic;
            var sensorName = topic.Split('/').Last();

           var message = e.ApplicationMessage.ConvertPayloadToString();
           Console.WriteLine($"{sensorName}-{message}");

           var tempSensorReading = System.Text.Json.JsonSerializer.Deserialize<TempSensorReading>(message);

           _influxRepo.WriteSensorData(
               measurement: tempSensorReading.Value,
               sensor: sensorName,
               timestamp: tempSensorReading.Timestamp);

            return Task.CompletedTask;
        };

        await mqttClient.ConnectAsync(options, CancellationToken.None);
        return mqttClient;
    }
}