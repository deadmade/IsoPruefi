using MQTT_Receiver_Worker.MQTT;

namespace MQTT_Receiver_Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Receiver _receiver;


    public Worker(ILogger<Worker> logger, Receiver receiver)
    {
        _logger = logger;
        _receiver = receiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _receiver.SubscribeToTopic();
    }
}