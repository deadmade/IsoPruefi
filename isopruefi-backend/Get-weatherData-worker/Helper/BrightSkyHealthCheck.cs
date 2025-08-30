using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Get_weatherData_worker.Helper;

public class BrightSkyHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BrightSkyHealthCheck> _logger;

    public BrightSkyHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration,
        ILogger<BrightSkyHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiUrl = _configuration["Weather:BrightSkyApiUrl"];
            if (string.IsNullOrEmpty(apiUrl))
                return HealthCheckResult.Unhealthy("BrightSky API URL configuration is missing");

            // Extract base URL for ping check
            var uri = new Uri(apiUrl.Replace("{lat}", "0").Replace("{lon}", "0"));
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Simple HEAD request to check if the service is reachable
            var response =
                await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, baseUrl), cancellationToken);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"BrightSky API is reachable (Status: {response.StatusCode})");
            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                // 4xx errors might be expected for HEAD requests, service is still reachable
                return HealthCheckResult.Healthy($"BrightSky API is reachable (Status: {response.StatusCode})");
            return HealthCheckResult.Unhealthy($"BrightSky API returned status code: {response.StatusCode}");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("BrightSky API health check timed out");
        }
    }
}