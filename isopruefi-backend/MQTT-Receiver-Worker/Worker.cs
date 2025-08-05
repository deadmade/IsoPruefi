using MQTT_Receiver_Worker.MQTT;
using MQTT_Receiver_Worker.MQTT.Interfaces;

namespace MQTT_Receiver_Worker;

/// <summary>
/// Background worker service for handling MQTT message receiving operations.
/// Implements a long-running service that subscribes to MQTT topics and processes messages.
/// </summary>
public class Worker : BackgroundService
{
    /// <summary>
    /// Logger for recording service operation information.
    /// </summary>
    private readonly ILogger<Worker> _logger;

    /// <summary>
    /// MQTT receiver component that handles topic subscriptions.
    /// </summary>
    private readonly IReceiver _receiver;
    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">Logger for recording service events.</param>
    /// <param name="receiver">MQTT receiver for subscribing to topics.</param>
    public Worker(ILogger<Worker> logger, IReceiver receiver)    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
    }

    /// <summary>
    /// Executes the worker process, subscribing to configured MQTT topics.
    /// This is the main entry point for the background service execution.
    /// </summary>
    /// <param name="stoppingToken">Token that can be used to request cancellation of the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _receiver.SubscribeToTopics();
    }
}