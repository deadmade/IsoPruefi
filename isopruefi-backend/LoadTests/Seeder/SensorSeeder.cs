using Database.EntityFramework;
using Database.EntityFramework.Enums;
using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoadTests.Seeder;

/// <summary>
///     Seeds test data for MQTT load testing including coordinates and topic settings
/// </summary>
public static class SensorSeeder
{
    /// <summary>
    ///     Seeds the database with test coordinates and topic settings for load testing
    /// </summary>
    /// <param name="serviceProvider">Service provider to resolve dependencies</param>
    /// <param name="sensorCount">Number of sensors to create for testing</param>
    /// <returns>List of created sensor names for use in load tests</returns>
    public static async Task<List<Tuple<string, string>>> SeedTestDataAsync(IServiceProvider serviceProvider,
        int sensorCount = 10)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sensorNames = new List<Tuple<string, string>>();

        // Create test coordinate mapping if it doesn't exist
        const int testPostalCode = 89518; // Heidenheim postal code for load testing
        var coordinateMapping = await context.CoordinateMappings
            .FirstOrDefaultAsync(c => c.PostalCode == testPostalCode);

        context.TopicSettings.RemoveRange(context.TopicSettings);
        await context.SaveChangesAsync();

        // Create topic settings for each test sensor
        for (var i = 1; i <= sensorCount; i++)
        {
            var sensorName = $"LoadTestSensor_{i:D3}";

            var topicSetting = new TopicSetting
            {
                CoordinateMappingId = coordinateMapping?.PostalCode ?? testPostalCode,
                DefaultTopicPath = "dhbw/ai/si2023/",
                GroupId = 2, // Using group 2 to match existing pattern
                SensorTypeEnum = SensorType.temp,
                SensorName = sensorName,
                SensorLocation = $"LoadTest Location {i}",
                HasRecovery = i % 3 == 0 // Every 3rd sensor has recovery enabled
            };

            context.TopicSettings.Add(topicSetting);
            var topic =
                $"{topicSetting.DefaultTopicPath}/{topicSetting.GroupId}/{topicSetting.SensorTypeEnum}/{topicSetting.SensorName}";
            sensorNames.Add(Tuple.Create(topic, sensorName));
        }

        await context.SaveChangesAsync();
        return sensorNames;
    }

    /// <summary>
    ///     Verifies that the expected number of test sensors exist in the database
    /// </summary>
    /// <param name="serviceProvider">Service provider to resolve dependencies</param>
    /// <param name="sensorCount">Expected number of sensors to validate</param>
    public static async Task CheckSensorExistsAsync(IServiceProvider serviceProvider, int sensorCount)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var count = await context.TopicSettings.CountAsync();
        Assert.That(count == sensorCount, "Expected sensor count does not match actual count");
    }

    /// <summary>
    ///     Cleans up test data after load tests complete
    /// </summary>
    /// <param name="serviceProvider">Service provider from the test factory</param>
    public static async Task CleanupTestDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Remove test topic settings
        var testTopicSettings = await context.TopicSettings
            .Where(ts => ts.SensorName != null && ts.SensorName.StartsWith("LoadTestSensor_"))
            .ToListAsync();

        context.TopicSettings.RemoveRange(testTopicSettings);
        await context.SaveChangesAsync();
    }
}