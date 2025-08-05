using Database.EntityFramework;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Microsoft.EntityFrameworkCore;
using Get_weatherData_worker.Helper;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Get_weatherData_worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddScoped<ISettingsRepo, SettingsRepo>();
        builder.Services.AddScoped<IInfluxRepo, InfluxRepo>();

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

        builder.Services.AddHostedService<Worker>();

        var app = builder.Build();

        // Configure health check endpoints
        app.MapHealthChecks("/api/health", new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        
        app.Run();
    }
}