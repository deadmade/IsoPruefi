using Database.Repository.InfluxRepo;
using InfluxDB3.Client.Write;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Database.Repository.InfluxRepo;

/// <summary>
/// Background service that periodically retries failed InfluxDB writes from the memory cache.
/// Runs every 5 minutes to attempt flushing cached PointData objects to InfluxDB.
/// </summary>
public class InfluxRetryService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<InfluxRetryService> _logger;
    private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Constructor for the InfluxRetryService.
    /// </summary>
    /// <param name="serviceScopeFactory">Factory for creating service scopes</param>
    /// <param name="logger">Logger instance</param>
    public InfluxRetryService(IServiceScopeFactory serviceScopeFactory, ILogger<InfluxRetryService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main execution loop for the background service.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InfluxDB retry service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RetryFailedWrites();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during InfluxDB retry operation");
            }

            await Task.Delay(_retryInterval, stoppingToken);
        }

        _logger.LogInformation("InfluxDB retry service stopped");
    }

    /// <summary>
    /// Attempts to retry all cached PointData writes to InfluxDB.
    /// </summary>
    private async Task RetryFailedWrites()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cachedInfluxRepo = scope.ServiceProvider.GetService<CachedInfluxRepo>();
        
        if (cachedInfluxRepo == null)
        {
            _logger.LogWarning("CachedInfluxRepo not available for retry operation");
            return;
        }

        var cachedPoints = cachedInfluxRepo.GetCachedPoints();
        
        if (cachedPoints.Count == 0)
        {
            _logger.LogDebug("No cached points to retry");
            return;
        }

        _logger.LogInformation("Attempting to retry {Count} cached InfluxDB writes", cachedPoints.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var kvp in cachedPoints)
        {
            try
            {
                await RetryPointWrite(cachedInfluxRepo, kvp.Key, kvp.Value);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retry cached point {CacheKey}", kvp.Key);
                failureCount++;
            }
        }

        _logger.LogInformation(
            "Retry operation completed. Success: {SuccessCount}, Failed: {FailureCount}", 
            successCount, failureCount);
    }

    /// <summary>
    /// Attempts to retry writing a single cached PointData object to InfluxDB.
    /// </summary>
    /// <param name="cachedRepo">The cached repository instance</param>
    /// <param name="cacheKey">Cache key for the point</param>
    /// <param name="point">The PointData to write</param>
    private async Task RetryPointWrite(CachedInfluxRepo cachedRepo, object cacheKey, PointData point)
    {
        try
        {
            // Use reflection to access the private _client field
            var clientField = typeof(CachedInfluxRepo).GetField("_client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (clientField?.GetValue(cachedRepo) is InfluxDB3.Client.InfluxDBClient client)
            {
                await client.WritePointAsync(point);
                cachedRepo.RemoveCachedPoint(cacheKey);
                
                _logger.LogDebug("Successfully retried cached point {CacheKey}", cacheKey);
            }
            else
            {
                _logger.LogError("Could not access InfluxDB client for retry operation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Retry failed for cached point {CacheKey}, will try again later", cacheKey);
            throw;
        }
    }
}