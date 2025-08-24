using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Database.EntityFramework;

/// <inheritdoc />
public class ApplicationDbContext : IdentityDbContext<ApiUser>
{
    //public DbSet<GeneralSetting> GeneralSettings { get; set; } = null!;

    /// <summary>
    /// Represents the collection of TopicSetting entities in the database.
    /// </summary>
    public DbSet<TopicSetting> TopicSettings { get; set; }

    /// <summary>
    /// Represents the collection of TokenInfo entities in the database.
    /// </summary>
    public DbSet<TokenInfo> TokenInfos { get; set; }

    /// <summary>
    /// Represents the collection of CoordinateMappings entities in the database.
    /// </summary>
    public DbSet<CoordinateMapping> CoordinateMappings { get; set; }

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
                    SensorName = "Sensor_One", SensorLocation = "North", HasRecovery = true
                },
                new TopicSetting
                {
                    TopicSettingId = 2, DefaultTopicPath = "dhbw/ai/si2023", GroupId = 2, SensorType = "temp",
                    SensorName = "Sensor_Two", SensorLocation = "South", HasRecovery = true
                }
            );
        });

        modelBuilder.Entity<CoordinateMapping>(b =>
        {
            b.HasKey(t => t.PostalCode);

            b.HasData(
                new CoordinateMapping
                {
                    PostalCode = 89518, Latitude = 48.6852, Longitude = 10.1287, Location = "Heidenheim an der Brenz"
                }
            );
        });
    }

    /// <summary>
    /// Applies any pending migrations for the specified DbContext.
    /// </summary>
    /// <param name="scope"></param>
    /// <typeparam name="TDbContext"></typeparam>
    public static void ApplyMigration<TDbContext>(IServiceScope scope)
        where TDbContext : DbContext
    {
        using var context = scope.ServiceProvider
            .GetRequiredService<TDbContext>();

        context.Database.Migrate();
    }
}