using System.Net;
using Database.EntityFramework;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MQTT_Receiver_Worker.MQTT;
using MQTT_Receiver_Worker.MQTT.Interfaces;

namespace MQTT_Receiver_Worker;

/// <summary>
/// This class is the entry point for the MQTT Receiver Worker application.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the MQTT Receiver Worker application.
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddMemoryCache();

        // Register Repos
        builder.Services.AddScoped<CachedInfluxRepo>();
        builder.Services.AddScoped<IInfluxRepo>(provider => provider.GetRequiredService<CachedInfluxRepo>());
        builder.Services.AddScoped<ISettingsRepo, SettingsRepo>();
        // Register Database with proper DbContext
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        // Register BusinessLogic
        builder.Services.AddSingleton<IReceiver, Receiver>();
        builder.Services.AddSingleton<IConnection, Connection>();

        builder.ConfigureHealthChecks();

        // Only in Development do we wire up the secret store:
        if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>();
        else if (builder.Environment.IsEnvironment("Docker")) builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddHostedService<Worker>();
        builder.Services.AddHostedService<InfluxRetryService>();

        var app = builder.Build();

        using var scope = ((IApplicationBuilder)app).ApplicationServices.CreateScope();
        ApplicationDbContext.ApplyMigration<ApplicationDbContext>(scope);

        //HealthCheck Middleware
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