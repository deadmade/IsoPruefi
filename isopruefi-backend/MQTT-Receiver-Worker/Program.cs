using Database.Repository.SettingsRepository;

namespace MQTT_Receiver_Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();
        
        // Register Repos
        builder.Services.AddSingleton<ISettingsRepo, SettingsRepo>();
        
        // Register BuisnessLogic
        

        var host = builder.Build();
        host.Run();
    }
}