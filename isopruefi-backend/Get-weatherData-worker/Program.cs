using Database.EntityFramework;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Microsoft.EntityFrameworkCore;

namespace Get_weatherData_worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        // Register Database with proper DbContext
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
        builder.Services.AddScoped<IInfluxRepo, InfluxRepo>();

        // Register Repos
        builder.Services.AddScoped<ISettingsRepo, SettingsRepo>();

        // Register Business Logic
        builder.Services.AddHttpClient();

        // Only in Development do we wire up the secret store:
        if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>();

        var host = builder.Build();
        host.Run();
    }
}