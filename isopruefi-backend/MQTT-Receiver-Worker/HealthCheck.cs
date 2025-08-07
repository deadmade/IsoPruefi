using Database.Repository.InfluxRepo;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MQTT_Receiver_Worker.MQTT;

namespace MQTT_Receiver_Worker;

public static class HealthCheck
{
    public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, "select 1",
                name: "PostgreSQL", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Database" })
            .AddCheck<MqttHealthCheck>("MQTT Connection", HealthStatus.Unhealthy, new[] { "MQTT" })
            .AddCheck<InfluxHealthCheck>("InfluxDB", HealthStatus.Unhealthy, new[] { "Database" });
    }
}