using MQTT_Receiver_Worker.MQTT.Interfaces;

namespace MQTT_Receiver_Worker;

/// <summary>
///     Background worker service for handling MQTT message receiving operations.
///     Implements a long-running service that subscribes to MQTT topics and processes messages.
/// </summary>
public class Worker : BackgroundService
{
    /// <summary>
    ///     MQTT connection component that handles broker connectivity.
    /// </summary>
    private readonly IConnection _connection;

    /// <summary>
    ///     Interval for monitoring connection health.
    /// </summary>
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Logger for recording service operation information.
    /// </summary>
    private readonly ILogger<Worker> _logger;

    /// <summary>
    ///     MQTT receiver component that handles topic subscriptions.
    /// </summary>
    private readonly IReceiver _receiver;

    /// <summary>
    ///     Time to wait before retrying connection attempts.
    /// </summary>
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Initializes a new instance of the <see cref="Worker" /> class.
    /// </summary>
    /// <param name="logger">Logger for recording service events.</param>
    /// <param name="receiver">MQTT receiver for subscribing to topics.</param>
    public Worker(ILogger<Worker> logger, IReceiver receiver, IConnection connection)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    ///     Executes the worker process, maintaining MQTT connection and subscribing to topics.
    ///     This is the main entry point for the background service execution.
    /// </summary>
    /// <param name="stoppingToken">Token that can be used to request cancellation of the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                if (!_connection.IsConnected)
                {
                    _logger.LogInformation("MQTT not connected. Attempting to establish connection...");
                    var connected = await _connection.TryConnectAsync();

                    if (!connected)
                    {
                        _logger.LogWarning("Failed to connect to MQTT. Retrying in {Delay}", _retryDelay);
                        await Task.Delay(_retryDelay, stoppingToken);
                        continue;
                    }
                }

                // Try to subscribe to topics
                await _receiver.SubscribeToTopics();
                _logger.LogInformation("Successfully subscribed to MQTT topics");

                // Monitor connection health
                await MonitorConnection(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MQTT Worker stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MQTT Worker. Retrying in {Delay}", _retryDelay);
                await Task.Delay(_retryDelay, stoppingToken);
            }

        await _connection.DisconnectAsync();
        _logger.LogInformation("MQTT Worker stopped");
    }

    /// <summary>
    ///     Monitors the MQTT connection health and detects disconnections.
    /// </summary>
    /// <param name="stoppingToken">Token that can be used to request cancellation of the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task MonitorConnection(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _connection.IsConnected)
        {
            await Task.Delay(_healthCheckInterval, stoppingToken);

            if (!_connection.IsConnected)
            {
                _logger.LogWarning("MQTT connection lost. Will attempt to reconnect...");
                break;
            }
        }
    }
}