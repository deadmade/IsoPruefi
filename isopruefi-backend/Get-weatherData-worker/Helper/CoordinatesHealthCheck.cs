using Database.EntityFramework;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Get_weatherData_worker.Helper;

/// <summary>
/// Healthcheck for availability of the database.
/// </summary>
public class CoordinatesHealthCheck : IHealthCheck
{
    /// <summary>
    /// Instance of the service provider for accessing services.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// Logger instance used to document diagnostics.
    /// </summary>
    private readonly ILogger<CoordinatesHealthCheck> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CoordinatesHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for accessing services.</param>
    /// <param name="logger">Logger for documenting diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown if configuration is missing.</exception>
    public CoordinatesHealthCheck(IServiceProvider serviceProvider, ILogger<CoordinatesHealthCheck> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Performs healthcheck for the database.
    /// </summary>
    /// <param name="context">Context when executing.</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>An asynchronous task representing the health check operation.</returns>
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
            return HealthCheckResult.Unhealthy("Cannot connect to database for coordinates service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during coordinates database health check");
            return HealthCheckResult.Unhealthy($"Database connectivity error: {ex.Message}");
        }
    }
}