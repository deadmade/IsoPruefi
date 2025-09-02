using Database.EntityFramework;
using Database.Repository.InfluxRepo.Influx;
using Database.Repository.InfluxRepo.InfluxCache;
using HdrHistogram.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoadTests.Seeder;

public static class InfluxSeeder
{
    public static async Task CreateIsoPrüfiDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CachedInfluxRepo>();   
        
        await context.WriteUptime("Seeder", (long)DateTime.UtcNow.SecondsSinceUnixEpoch());
        await context.WriteOutsideWeatherData("Heidenheim", "example.com", 20.0, DateTime.UtcNow, 89518);
        await context.WriteSensorData(22.5, "Seeder", (long)DateTime.UtcNow.SecondsSinceUnixEpoch(), 1);
    }
    
    public static async Task CheckDatabaseExists(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CachedInfluxRepo>();   
        
        await foreach (var row in context.GetUptime("Seeder"))
        {
            Assert.That(row != null, "Database does not exist");
        }
    }
}