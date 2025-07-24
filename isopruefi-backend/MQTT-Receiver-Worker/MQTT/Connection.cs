using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepository;
using Microsoft.Extensions.Logging;
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

    private IInfluxRepo _influxRepo;
    private MqttClientOptions _options;
    private IMqttClient _mqttClient;
    private readonly ILogger<Connection> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="influxRepo">Repository for writing sensor data to InfluxDB</param>
    /// <param name="logger">Logger for recording connection events</param>
    public Connection(ILogger<Connection> logger ,IInfluxRepo influxRepo)
    {
        _influxRepo = influxRepo;
        _logger = logger;
        InitialMqttConfig();
    }

    /// <summary>
    /// Initializes the MQTT client configuration with default settings.
    /// </summary>
    private void InitialMqttConfig()
    {
        const string broker = "aicon.dhbw-heidenheim.de";
        const int port = 1883;
        var clientId = Guid.NewGuid().ToString();

        _logger.LogDebug("Initializing MQTT client with broker: {Broker}:{Port}, ClientId: {ClientId}", broker, port, clientId);

        // Create a MQTT client factory
        var factory = new MqttClientFactory();

        // Create a MQTT client instance
        _mqttClient = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port) // MQTT broker address and port
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId(clientId)
            .Build();
    }

    /// <summary>
    /// Establishes a connection to the MQTT broker and configures message handlers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the connected MQTT client.</returns>
    public async Task<IMqttClient> GetConnection()
    {
        _logger.LogInformation("Establishing connection to MQTT broker");
        _mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;
        _mqttClient.DisconnectedAsync += Disconnected;

        await _mqttClient.ConnectAsync(_options, CancellationToken.None);
        _logger.LogInformation("Successfully connected to MQTT broker");
        return _mqttClient;
    }

    private async Task Disconnected (MqttClientDisconnectedEventArgs e)
    {
        _logger.LogWarning("Disconnected from MQTT broker. Attempting to reconnect...");
        try
        {
            await _mqttClient.ConnectAsync(_options, CancellationToken.None);
            _logger.LogInformation("Reconnected successfully to MQTT broker");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconnection to MQTT broker failed");
        }
    }

    private Task<Task> ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var sensorName = topic.Split('/').Last();

        var message = e.ApplicationMessage.ConvertPayloadToString();
        _logger.LogInformation("Received message from sensor {SensorName}: {Message}", sensorName, message);

        var tempSensorReading = System.Text.Json.JsonSerializer.Deserialize<TempSensorReading>(message);

        if (tempSensorReading == null || tempSensorReading.Value.Length == 0)
        {
            _logger.LogWarning("Received null or empty sensor reading from {SensorName}. Skipping processing", sensorName);
            return Task.FromResult(Task.CompletedTask);
        }

        if (tempSensorReading.Value.Length > 1)
        {
            _logger.LogInformation("Received multiple values in sensor reading from {SensorName}. Only the first value will be processed", sensorName);
            return Task.FromResult(Task.CompletedTask);
        }

        _influxRepo.WriteSensorData(
            tempSensorReading.Value[0],
            sensorName,
            tempSensorReading.Timestamp,
            sequence: tempSensorReading.Sequence);

        return Task.FromResult(Task.CompletedTask);
    }
}