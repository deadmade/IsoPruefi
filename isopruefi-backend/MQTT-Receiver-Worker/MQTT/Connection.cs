using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
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
    private IServiceProvider _serviceProvider;
    private MqttClientOptions _options;
    private IMqttClient _mqttClient;
    private readonly ILogger<Connection> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="influxRepo">Repository for writing sensor data to InfluxDB</param>
    /// <param name="logger">Logger for recording connection events</param>
    /// <param name="configuration">Configuration for MQTT settings</param>
    public Connection(ILogger<Connection> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        InitialMqttConfig();
    }

    /// <summary>
    /// Initializes the MQTT client configuration with settings from configuration.
    /// </summary>
    private void InitialMqttConfig()
    {
        var broker = _configuration["Mqtt:BrokerHost"];
        var port = _configuration.GetValue<int>("Mqtt:BrokerPort", 1883);
        var clientId = Guid.NewGuid().ToString();

        _logger.LogDebug("Initializing MQTT client with broker: {Broker}:{Port}, ClientId: {ClientId}", broker, port,
            clientId);

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

    private async Task Disconnected(MqttClientDisconnectedEventArgs e)
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
        using var scope = _serviceProvider.CreateScope();
        var influxRepo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();

        var topic = e.ApplicationMessage.Topic;
        var sensorName = topic.Split('/').Last();

        var message = e.ApplicationMessage.ConvertPayloadToString();
        _logger.LogInformation("Received message from sensor {SensorName}: {Message}", sensorName, message);

        try
        {


        var tempSensorReading = System.Text.Json.JsonSerializer.Deserialize<TempSensorReading>(message);

        if (tempSensorReading == null || tempSensorReading.Value == null)
        {
            _logger.LogWarning("Received null sensor reading from {SensorName}. Skipping processing",
                sensorName);
            return Task.FromResult(Task.CompletedTask);
        }
        
        if (tempSensorReading.Value.Length == 0)
        {
            _logger.LogWarning("Received empty sensor reading from {SensorName}. Skipping processing",
                sensorName);
            return Task.FromResult(Task.CompletedTask);
        }
        
        if (tempSensorReading.Value.Length == 1 && tempSensorReading.Value[0] != null && tempSensorReading.Meta == null)
        {
            influxRepo.WriteSensorData(
                tempSensorReading.Value[0] ?? 0,
                sensorName,
                tempSensorReading.Timestamp,
                tempSensorReading.Sequence ?? 0);
        }
        else if (tempSensorReading.Value.Length > 1)
        {
            _logger.LogInformation(
                "Received multiple values in sensor reading from {SensorName}. Only the first value will be processed",
                sensorName);
            return Task.FromResult(Task.CompletedTask);
        }
        else if (tempSensorReading is { Sequence: null, Meta: not null })
        {
            foreach (var reading in tempSensorReading.Meta)
            {
                if (reading.Value == null || reading.Value.Length == 0)
                {
                    _logger.LogWarning("Received empty value in sensor reading from {SensorName}. Skipping processing",
                        sensorName);
                    continue;
                }
                
                influxRepo.WriteSensorData(
                    reading.Value[0] ?? 0,
                    sensorName,
                    reading.Timestamp,
                    reading.Sequence ?? 0);
            }
        }
        else
        {
            _logger.LogWarning("Received sensor reading with unexpected format from {SensorName}. Skipping processing",
                sensorName);
            return Task.FromResult(Task.CompletedTask);
        }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error processing message from sensor {SensorName}: {Message}", sensorName, message);
            return Task.FromResult(Task.CompletedTask);
        }

        return Task.FromResult(Task.CompletedTask);
    }
}