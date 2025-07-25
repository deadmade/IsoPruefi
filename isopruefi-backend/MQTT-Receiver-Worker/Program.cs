using Database.EntityFramework;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using MQTT_Receiver_Worker.MQTT;

namespace MQTT_Receiver_Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        // Register Database
        builder.Services.AddSingleton<SettingsContext>();
        builder.Services.AddSingleton<IInfluxRepo, InfluxRepo>();

        // Register Repos
        builder.Services.AddSingleton<ISettingsRepo, SettingsRepo>();

        // Register BusinessLogic
        builder.Services.AddSingleton<Receiver>();
        builder.Services.AddSingleton<Connection>();

        // Only in Development do we wire up the secret store:
        if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>();
        else if (builder.Environment.IsEnvironment("Docker")) builder.Configuration.AddEnvironmentVariables();

        var host = builder.Build();
        host.Run();
    }
}