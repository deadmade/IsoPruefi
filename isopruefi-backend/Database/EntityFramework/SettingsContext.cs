using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework;

public class SettingsContext : DbContext
{
    //public DbSet<GeneralSetting> GeneralSettings { get; set; } = null!;
    public DbSet<TopicSetting> TopicSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TopicSetting>(b =>
        {
            b.HasKey(t => t.TopicSettingId);
            b.Property(t => t.TopicSettingId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            b.HasData(
                new TopicSetting
                {
                    TopicSettingId = 1, DefaultTopicPath = "dhbw/ai/si2023", GroupId = 2, SensorType = "temp",
                    SensorName = "Sensor_One", SensorLocation = "North"
                },
                new TopicSetting
                {
                    TopicSettingId = 2, DefaultTopicPath = "dhbw/ai/si2023", GroupId = 2, SensorType = "temp",
                    SensorName = "Sensor_Two", SensorLocation = "South"
                }
            );
        });
    }

    //TODO: Set connection from environment variable or configuration file
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            @"Host=localhost:5432;Username=Isopruefi;Password=secret;Database=Isopruefi");
    }
}