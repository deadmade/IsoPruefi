using System.Net;
using Database.EntityFramework;
using Database.Repository.CoordinateRepo;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Get_weatherData_worker.Helper;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Get_weatherData_worker;

/// <summary>
/// This class is the entry point for the Get Weather Data Worker application.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the Get Weather Data Worker application.
    /// </summary>
    /// <param name="args">Arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddScoped<ISettingsRepo, SettingsRepo>();
        builder.Services.AddScoped<IInfluxRepo, InfluxRepo>();
        builder.Services.AddScoped<ICoordinateRepo, CoordinateRepo>();

        // Register Database with proper DbContext
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        // Register Business Logic
        builder.Services.AddHttpClient();

        // Configure health checks
        builder.ConfigureHealthChecks();

        // Only in Development do we wire up the secret store:
        if (builder.Environment.IsDevelopment())
            builder.Configuration.AddUserSecrets<Program>();
        else if (builder.Environment.IsEnvironment("Docker")) builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddHostedService<Worker>();

        var app = builder.Build();

        using var scope = ((IApplicationBuilder)app).ApplicationServices.CreateScope();
        ApplicationDbContext.ApplyMigration<ApplicationDbContext>(scope);

        // Configure health check endpoints
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseHealthChecksPrometheusExporter("/healthoka",
            options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK);

        app.Run();
    }
}