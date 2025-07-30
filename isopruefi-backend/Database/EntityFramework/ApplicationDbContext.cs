using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework;

/// <inheritdoc />
public class ApplicationDbContext : IdentityDbContext<ApiUser>
{
    //public DbSet<GeneralSetting> GeneralSettings { get; set; } = null!;

    /// <summary>
    /// Represents the collection of TopicSetting entities in the database.
    /// </summary>
    public virtual DbSet<TopicSetting> TopicSettings { get; set; }

    /// <summary>
    /// Represents the collection of TokenInfo entities in the database.
    /// </summary>
    public virtual DbSet<TokenInfo> TokenInfos { get; set; }

    /// <inheritdoc />
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
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
}