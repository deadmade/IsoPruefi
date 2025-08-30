using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Database.Repository.InfluxRepo;

/// <summary>
///     Health check for InfluxDB connectivity.
/// </summary>
public class InfluxHealthCheck : IHealthCheck
{
    private readonly IInfluxRepo _influxRepo;
    private readonly ILogger<InfluxHealthCheck> _logger;

    /// <summary>
    ///     Constructor for the InfluxHealthCheck class.
    /// </summary>
    /// <param name="influxRepo">The InfluxDB repository.</param>
    /// <param name="logger">The logger instance.</param>
    public InfluxHealthCheck(IInfluxRepo influxRepo, ILogger<InfluxHealthCheck> logger)
    {
        _influxRepo = influxRepo ?? throw new ArgumentNullException(nameof(influxRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Checks the health of the InfluxDB connection.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A health check result indicating the status of InfluxDB.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a simple query to check connectivity
            var testStart = DateTime.UtcNow.AddMinutes(-1);
            var testEnd = DateTime.UtcNow;
            var testSensor = "Sensor_One";

            // Use a simple query that should work regardless of data presence
            var query = _influxRepo.GetSensorWeatherData(testStart, testEnd, testSensor);

            // Try to get the first result or complete if empty
            await using var enumerator = query.GetAsyncEnumerator(cancellationToken);

            // Just checking if we can connect and execute a query
            // We don't need actual data, just successful connection
            var hasData = await enumerator.MoveNextAsync();

            _logger.LogDebug("InfluxDB health check completed successfully. Has data: {HasData}", hasData);
            return HealthCheckResult.Healthy("InfluxDB connection is healthy");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("InfluxDB health check was cancelled");
            return HealthCheckResult.Unhealthy("InfluxDB health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InfluxDB health check failed");
            return HealthCheckResult.Unhealthy($"InfluxDB connection failed: {ex.Message}", ex);
        }
    }
}