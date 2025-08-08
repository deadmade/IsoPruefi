using Database.EntityFramework;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Get_weatherData_worker.Helper;

public class CoordinatesHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CoordinatesHealthCheck> _logger;

    public CoordinatesHealthCheck(IServiceProvider serviceProvider, ILogger<CoordinatesHealthCheck> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Simple database connectivity check
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
                return HealthCheckResult.Healthy("Database is reachable for coordinates service");
            else
                return HealthCheckResult.Unhealthy("Cannot connect to database for coordinates service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during coordinates database health check");
            return HealthCheckResult.Unhealthy($"Database connectivity error: {ex.Message}");
        }
    }
}