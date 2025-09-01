using Database.EntityFramework;
using Database.Repository.CoordinateRepo;
using Database.Repository.InfluxRepo;
using Database.Repository.InfluxRepo.InfluxCache;
using Database.Repository.SettingsRepo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTT_Receiver_Worker.MQTT;
using MQTT_Receiver_Worker.MQTT.Interfaces;
using Rest_API;
using Rest_API.Models;
using Testcontainers.InfluxDb;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Infrastructure;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15")
        .WithDatabase("isopruefi_integration_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly InfluxDbContainer _influxDbContainer = new InfluxDbBuilder()
        .WithImage("influxdb:3.2.1")
        .WithUsername("test")
        .WithPassword("test")
        .WithOrganization("isopruefi_org")
        .WithBucket("isopruefi_bucket")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.IntegrationTests.json");

            // Add in-memory configuration to override admin settings for integration tests
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Admin:UserName"] = "integrationtestadmin",
                ["Admin:Email"] = "integrationtestadmin@test.com",
                ["Admin:Password"] = "IntegrationTestAdmin123!"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            services.Configure<LoggerFilterOptions>(options => { options.MinLevel = LogLevel.Warning; });

            // Register MQTT-related services for testing
            services.AddMemoryCache();
            services.AddScoped<CachedInfluxRepo>();
            services.AddScoped<IInfluxRepo>(provider => provider.GetRequiredService<CachedInfluxRepo>());
            services.AddScoped<ISettingsRepo, SettingsRepo>();
            services.AddSingleton<IReceiver, Receiver>();
            services.AddSingleton<IConnection, Connection>();

            // Register weather worker related services for testing
            services.AddScoped<ICoordinateRepo, CoordinateRepo>();
            services.AddHttpClient();
        });

        // Disable HTTPS redirection for integration tests
        builder.UseSetting("HTTPS_REDIRECTION", "false");
        builder.UseEnvironment("Testing");
    }

    public new HttpClient CreateClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task StartAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Seed roles
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { Roles.Admin, Roles.User };

        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            try
            {
                _dbContainer?.StopAsync().GetAwaiter().GetResult();
                _dbContainer?.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (ObjectDisposedException)
            {
                // Container already disposed, ignore
            }

        base.Dispose(disposing);
    }
}