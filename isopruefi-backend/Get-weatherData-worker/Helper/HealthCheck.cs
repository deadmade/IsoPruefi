using Database.Repository.InfluxRepo.Influx;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Get_weatherData_worker.Helper;

/// <summary>
///     Provides extension methods for configuring application health checks.
/// </summary>
public static class HealthCheck
{
    /// <summary>
    ///     Registers and configures health checks for the application.
    /// </summary>
    /// <param name="builder"> The WebApplicationBuilder used to configure services and middleware for the application.</param>
    public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                "select 1",
                name: "PostgreSQL",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "Database" })
            .AddCheck<OpenMeteoHealthCheck>("OpenMeteo API",
                HealthStatus.Unhealthy,
                new[] { "WeatherAPI", "External" })
            .AddCheck<BrightSkyHealthCheck>("BrightSky API",
                HealthStatus.Unhealthy,
                new[] { "WeatherAPI", "External" })
            .AddCheck<CoordinatesHealthCheck>("Coordinates Database",
                HealthStatus.Unhealthy,
                new[] { "Coordinates", "External" })
            .AddCheck<InfluxHealthCheck>("InfluxDB",
                HealthStatus.Unhealthy,
                new[] { "Database" });
    }
}