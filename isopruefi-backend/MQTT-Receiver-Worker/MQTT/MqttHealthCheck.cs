using Microsoft.Extensions.Diagnostics.HealthChecks;
using MQTT_Receiver_Worker.MQTT.Interfaces;

namespace MQTT_Receiver_Worker.MQTT;

public class MqttHealthCheck : IHealthCheck
{
    private readonly IConnection _connection;

    public MqttHealthCheck(IConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return _connection.IsConnected
                ? HealthCheckResult.Healthy("MQTT connection is active")
                : HealthCheckResult.Unhealthy("MQTT connection is not active");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error checking MQTT connection", ex);
        }
    }
}