using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework;

public class ApplicationDbContext : IdentityDbContext<ApiUser>
{
    //public DbSet<GeneralSetting> GeneralSettings { get; set; } = null!;
    public DbSet<TopicSetting> TopicSettings { get; set; }
    public DbSet<TokenInfo> TokenInfos { get; set; }
    
    public DbSet<CoordinateMapping> CoordinateMappings { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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