using System.Text.Json;
using System.Text.Json.Serialization;
using Database.Repository.InfluxRepo;
using MQTT_Receiver_Worker.MQTT.Interfaces;
using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;
using MQTTnet.Formatter;

namespace MQTT_Receiver_Worker.MQTT;

/// <summary>
/// Handles MQTT broker connection and message processing.
/// Establishes a connection to the MQTT broker, subscribes to topics,
/// and processes incoming temperature sensor messages.
/// </summary>
public class Connection : IConnection
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttClientOptions _options;
    private IMqttClient? _mqttClient;
    private readonly ILogger<Connection> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private bool _isConnected = false;

    /// <summary>
    /// Gets a value indicating whether the MQTT client is currently connected.
    /// </summary>
    public bool IsConnected => _isConnected && _mqttClient?.IsConnected == true;

    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="logger">Logger for recording connection events</param>
    /// <param name="configuration">Configuration for MQTT settings</param>
    public Connection(ILogger<Connection> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _options = CreateMqttOptions();

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Creates the MQTT client configuration with settings from configuration.
    /// </summary>
    private MqttClientOptions CreateMqttOptions()
    {
        var broker = _configuration["Mqtt:BrokerHost"];
        var port = _configuration.GetValue("Mqtt:BrokerPort", 1883);
        var clientId = Guid.NewGuid().ToString();

        _logger.LogDebug("Creating MQTT client options with broker: {Broker}:{Port}, ClientId: {ClientId}", broker,
            port, clientId);

        return new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId(clientId)
            .Build();
    }

    /// <summary>
    /// Attempts to connect to the MQTT broker.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the connection was successful.</returns>
    public async Task<bool> TryConnectAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            if (IsConnected) return true;

            _logger.LogInformation("Attempting to connect to MQTT broker");

            if (_mqttClient == null)
            {
                var factory = new MqttClientFactory();
                _mqttClient = factory.CreateMqttClient();
            }

            _mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;
            _mqttClient.DisconnectedAsync += Disconnected;

            await _mqttClient.ConnectAsync(_options, CancellationToken.None);
            _isConnected = true;
            _logger.LogInformation("Successfully connected to MQTT broker");
            return true;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex, "Failed to connect to MQTT broker");
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Establishes a connection to the MQTT broker and configures message handlers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the connected MQTT client, or null if connection failed.</returns>
    public async Task<IMqttClient?> GetConnectionAsync()
    {
        if (IsConnected) return _mqttClient;

        var connected = await TryConnectAsync();
        return connected ? _mqttClient : null;
    }

    /// <summary>
    /// Disconnects from the MQTT broker.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DisconnectAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            if (_mqttClient?.IsConnected == true)
            {
                _logger.LogInformation("Disconnecting from MQTT broker");
                await _mqttClient.DisconnectAsync();
            }

            _isConnected = false;
            _logger.LogInformation("Disconnected from MQTT broker");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MQTT disconnect");
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task Disconnected(MqttClientDisconnectedEventArgs e)
    {
        _isConnected = false;
        _logger.LogWarning("Disconnected from MQTT broker: {Reason}", e.Reason);

        // Don't immediately reconnect here - let the worker handle it with proper timing
    }

    private async Task<Task> ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var influxRepo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();

            var topic = e.ApplicationMessage.Topic;
            var topics = topic.Split('/');
            var sensorName = topics.Last();

            var message = e.ApplicationMessage.ConvertPayloadToString();
            _logger.LogInformation("Received message from sensor {SensorName}: {Message}", sensorName, message);

            var tempSensorReading = JsonSerializer.Deserialize<TempSensorReading>(message);

            if (tempSensorReading == null)
            {
                _logger.LogError("Failed to deserialize message from sensor {SensorName}. Skipping processing",
                    sensorName);
                return Task.FromResult(Task.CompletedTask);
            }

            if (sensorName != "recovered")
            {
                await influxRepo.WriteUptime(
                    sensorName,
                    tempSensorReading.Timestamp);
                return await ProcessSensorReading(tempSensorReading, sensorName, influxRepo);
            }
            var recoveredSensorName = topics.ElementAtOrDefault(topics.Length - 2);

            if (recoveredSensorName != null)
                return await ProcessBatchSensorReading(tempSensorReading, recoveredSensorName, influxRepo);

            _logger.LogError("Recovered sensor name is null. Skipping processing");
            return Task.FromResult(Task.CompletedTask);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error processing message from MQTT broker");
        }

        return Task.FromResult(Task.CompletedTask);
    }

    private async Task<Task> ProcessSensorReading(TempSensorReading tempSensorReading, string sensorName,
        IInfluxRepo influxRepo)
    {
        if (tempSensorReading.Value == null)
        {
            _logger.LogWarning("Received null sensor reading from {SensorName}. Skipping processing",
                sensorName);
            return Task.FromResult(Task.CompletedTask);
        }

        switch (tempSensorReading.Value.Length)
        {
            case 0:
                _logger.LogWarning("Received empty sensor reading from {SensorName}. Skipping processing",
                    sensorName);
                break;
            case 1 when tempSensorReading.Value[0] != null 
                        && (tempSensorReading.Meta is null || (tempSensorReading.Meta.Value is null && 
                            tempSensorReading.Meta.Timestamp is null && tempSensorReading.Meta.Sequence is null)):
                await influxRepo.WriteSensorData(
                    tempSensorReading.Value[0] ?? 0,
                    sensorName,
                    tempSensorReading.Timestamp,
                    tempSensorReading.Sequence ?? 0);
                break;
            case > 1:
                _logger.LogInformation(
                    "Received multiple values in sensor reading from {SensorName}. Only the first value will be processed",
                    sensorName);
                break;
            default:
                _logger.LogError(
                    "Received sensor reading with unexpected format from {SensorName}. Skipping processing",
                    sensorName);
                break;
        }

        return Task.FromResult(Task.CompletedTask);
    }

    private async Task<Task> ProcessBatchSensorReading(TempSensorReading tempSensorReading, string sensorName,
        IInfluxRepo influxRepo)
    {
        if (tempSensorReading is { Sequence: not null, Meta: null, Value: not null, Value.Length: > 0 })
        {
            _logger.LogWarning("Received sensor reading with unexpected format from {SensorName}. Skipping processing",
                sensorName);
            return Task.FromResult(Task.CompletedTask);
        }

        if (tempSensorReading.Meta is { Timestamp: null, Sequence: null, Value: null })
        {
            _logger.LogWarning("Received sensor reading with unexpected format in meta from {SensorName}. Skipping processing",
                sensorName);
            return Task.FromResult(Task.CompletedTask);
        }

        if (tempSensorReading.Meta.Sequence.Length != 0
            && tempSensorReading.Meta.Sequence.Length == tempSensorReading.Meta.Timestamp.Length
            && tempSensorReading.Meta.Sequence.Length == tempSensorReading.Meta.Value.Length)
        {
            var tempDataMeta = tempSensorReading.Meta;
            await Parallel.ForEachAsync(Enumerable.Range(0, tempDataMeta.Sequence.Length - 1), 
                async (value, cancellationToken) =>
                {
                    double?[]? values = { tempDataMeta.Value[value] };
                    var tempData = new TempSensorReading{
                        Timestamp = tempDataMeta.Timestamp[value],
                        Value = values,
                        Sequence = tempDataMeta.Sequence[value],
                        Meta = null
                    };
                    await ProcessSensorReading(tempData, sensorName, influxRepo);
                });
        }
        else
        {
            _logger.LogWarning("Sensor Meta Data contains unexpected format");
        }

        return Task.FromResult(Task.CompletedTask);
    }
}