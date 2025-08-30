using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Get_weatherData_worker.Helper;

/// <summary>
/// Healthcheck for availability of Meteo API.
/// </summary>
public class OpenMeteoHealthCheck : IHealthCheck
{
    /// <summary>
    /// Factory used for making API requests.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;
    
    /// <summary>
    /// Configuration used to retrieve settings.
    /// </summary>
    private readonly IConfiguration _configuration;
    
    /// <summary>
    /// Logger instance used to document diagnostics.
    /// </summary>
    private readonly ILogger<OpenMeteoHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenMeteoHealthCheck"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for API calls.</param>
    /// <param name="configuration">Configuration for settings.</param>
    /// <param name="logger">Logger for documenting diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown if configuration is missing.</exception>
    public OpenMeteoHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration,
        ILogger<OpenMeteoHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs the healthcheck on the Meteo API.
    /// </summary>
    /// <param name="context">Context when executing.</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>An asynchronous task representing the health check operation.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiUrl = _configuration["Weather:OpenMeteoApiUrl"];
            if (string.IsNullOrEmpty(apiUrl))
                return HealthCheckResult.Unhealthy("OpenMeteo API URL configuration is missing");

            // Extract base URL for ping check
            var uri = new Uri(apiUrl.Replace("{lat}", "0").Replace("{lon}", "0"));
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Simple HEAD request to check if the service is reachable
            var response =
                await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, baseUrl), cancellationToken);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"OpenMeteo API is reachable (Status: {response.StatusCode})");
            else if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                // 4xx errors might be expected for HEAD requests, service is still reachable
                return HealthCheckResult.Healthy($"OpenMeteo API is reachable (Status: {response.StatusCode})");
            else
                return HealthCheckResult.Unhealthy($"OpenMeteo API returned status code: {response.StatusCode}");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy(
                "OpenMeteo API health check was not successful, possibly due to a timeout or network issue");
        }
    }
}