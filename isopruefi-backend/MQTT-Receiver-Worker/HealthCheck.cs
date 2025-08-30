using Database.Repository.InfluxRepo;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MQTT_Receiver_Worker.MQTT;

namespace MQTT_Receiver_Worker;

/// <summary>
/// Provides extension methods for configuring application health checks.
/// </summary>
public static class HealthCheck
{
    /// <summary>
    /// Registers and configures health checks for the application.
    /// </summary>
    /// <param name="builder"> The WebApplicationBuilder used to configure services and middleware for the application.</param>
    public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, "select 1",
                name: "PostgreSQL", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Database" })
            .AddCheck<MqttHealthCheck>("MQTT Connection", HealthStatus.Unhealthy, new[] { "MQTT" })
            .AddCheck<CachedInfluxHealthCheck>("InfluxDB", HealthStatus.Unhealthy, new[] { "Database" });
    }
}