using Database.Repository.InfluxRepo;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rest_API.Helper;

public static class HealthCheck
{
    public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, "select 1",
                name: "Postgress", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Database" })
            .AddCheck<InfluxHealthCheck>("InfluxDB", HealthStatus.Unhealthy, new[] { "Database" });
    }
}