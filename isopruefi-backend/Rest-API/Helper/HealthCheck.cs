using Database.Repository.InfluxRepo.Influx;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rest_API.Helper;

/// <summary>
///     Provides extension methods for configuring application healthchecks.
/// </summary>
public static class HealthCheck
{
    /// <summary>
    /// Registers and configures health checks for the application.
    /// </summary>
    public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, "select 1",
                name: "PostgreSQL", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Database" })
            .AddCheck<InfluxHealthCheck>("InfluxDB", HealthStatus.Unhealthy, new[] { "Database" });
    }
}