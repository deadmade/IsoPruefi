using Microsoft.Extensions.Diagnostics.HealthChecks;
using MQTT_Receiver_Worker.MQTT.Interfaces;

namespace MQTT_Receiver_Worker.MQTT;

/// <summary>
///     Checking the health of the MQTT connection-
/// </summary>
public class MqttHealthCheck : IHealthCheck
{
    /// <summary>
    ///     MQTT Connection instance used to verify the health status.
    /// </summary>
    private readonly IConnection _connection;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttHealthCheck"/> class.
    /// </summary>
    /// <param name="connection">Connection used to determine health status</param>
    /// <exception cref="ArgumentNullException">Thrown if connection is null</exception>
    public MqttHealthCheck(IConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    ///     Performs the health check for the MQTT connection.
    /// </summary>
    /// <param name="context">Context in which the check is executed.</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>A task that represents the asynchronous health check.</returns>
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