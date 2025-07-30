using Database.EntityFramework;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Microsoft.EntityFrameworkCore;
using MQTT_Receiver_Worker.MQTT;

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
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddScoped<IInfluxRepo, InfluxRepo>();

        // Register Repos
        builder.Services.AddScoped<ISettingsRepo, SettingsRepo>();

        // Register Database with proper DbContext
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        // Register BusinessLogic
        builder.Services.AddSingleton<Receiver>();
        builder.Services.AddSingleton<Connection>();

        // Only in Development do we wire up the secret store:
        if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>();
        else if (builder.Environment.IsEnvironment("Docker")) builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}