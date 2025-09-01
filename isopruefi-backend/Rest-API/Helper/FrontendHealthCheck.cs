using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rest_API.Helper;

/// <summary>
/// Checking the health of the frontend page.
/// </summary>
public class FrontendHealthCheck : IHealthCheck
{
    /// <summary>
    /// Http Factory for calling the frontend URL.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;
    
    /// <summary>
    /// URL for reaching the frontend.
    /// </summary>
    private readonly string _frontendUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="FrontendHealthCheck"/> class.
    /// </summary>
    /// <param name="httpClientFactory">IHttpClientFactory</param>
    /// <param name="configuration">Configuration for getting the URL</param>
    public FrontendHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        
        _frontendUrl = configuration["Frontend:URL"]?? throw new InvalidOperationException(
            "Frontend:URL configuration is missing");
    }

    /// <summary>
    /// Performs the health check for the Frontend.
    /// </summary>
    /// <param name="context">Context in which the check is executed</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>An asynchronous task representing the healthcheck</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(_frontendUrl);
            if (response.IsSuccessStatusCode) return HealthCheckResult.Healthy("Frontend is healthy");
            return HealthCheckResult.Unhealthy("Frontend is unhealthy");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("Frontend is unhealthy", e);
        }
    }
}