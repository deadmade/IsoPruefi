using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Database.Repository.InfluxRepo;

/// <summary>
/// Enhanced health check for InfluxDB connectivity and cache status.
/// Provides information about both InfluxDB connectivity and cached writes.
/// </summary>
public class CachedInfluxHealthCheck : IHealthCheck
{
    private readonly CachedInfluxRepo _cachedInfluxRepo;
    private readonly ILogger<CachedInfluxHealthCheck> _logger;

    /// <summary>
    /// Constructor for the CachedInfluxHealthCheck class.
    /// </summary>
    /// <param name="cachedInfluxRepo">The cached InfluxDB repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CachedInfluxHealthCheck(CachedInfluxRepo cachedInfluxRepo, ILogger<CachedInfluxHealthCheck> logger)
    {
        _cachedInfluxRepo = cachedInfluxRepo ?? throw new ArgumentNullException(nameof(cachedInfluxRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks the health of the InfluxDB connection and cache status.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A health check result indicating the status of InfluxDB and cache.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cached points count
            var cachedPoints = _cachedInfluxRepo.GetCachedPoints();
            var cachedCount = cachedPoints.Count;

            // Perform a simple query to check InfluxDB connectivity
            var testStart = DateTime.UtcNow.AddMinutes(-1);
            var testEnd = DateTime.UtcNow;

            var query = _cachedInfluxRepo.GetSensorWeatherData(testStart, testEnd);
            await using var enumerator = query.GetAsyncEnumerator(cancellationToken);
            var hasData = await enumerator.MoveNextAsync();

            var data = new Dictionary<string, object>
            {
                ["influxdb_connected"] = true,
                ["cached_points_count"] = cachedCount,
                ["has_recent_data"] = hasData
            };

            if (cachedCount > 0)
            {
                _logger.LogWarning("InfluxDB is connected but {CachedCount} points are still cached", cachedCount);

                return HealthCheckResult.Degraded(
                    $"InfluxDB connected but {cachedCount} cached writes pending", 
                    data: data);
            }

            _logger.LogDebug("InfluxDB health check completed successfully. No cached points");
            return HealthCheckResult.Healthy("InfluxDB connection is healthy, no cached writes", data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("InfluxDB health check timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"InfluxDB connection failed: {ex.Message}.", ex);
        }
    }
}