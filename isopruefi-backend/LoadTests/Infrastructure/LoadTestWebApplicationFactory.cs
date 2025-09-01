using Database.EntityFramework;
using Database.Repository.CoordinateRepo;
using Database.Repository.InfluxRepo;
using Database.Repository.InfluxRepo.InfluxCache;
using Database.Repository.SettingsRepo;
using DotNet.Testcontainers.Containers;
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

namespace LoadTests.Infrastructure;

/// <summary>
/// Web application factory for load tests using TestContainers
/// </summary>
public class LoadTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly InfluxDbContainer _influxDbContainer;
    private readonly IContainer _mqttContainer;

    public LoadTestWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:alpine3.21")
            .WithDatabase("isopruefi_loadtest")
            .WithUsername("loadtest")
            .WithPassword("LoadTest123!")
            .Build();

        _influxDbContainer = new InfluxDbBuilder()
            .WithImage("influxdb:3.2")
            .WithUsername("loadtestadmin")
            .WithPassword("LoadTestAdmin123!")
            .WithOrganization("loadtest-org")
            .WithBucket("loadtest-bucket")
            .Build();

        _mqttContainer = MqttContainer.Create();
    }

    public string DatabaseConnectionString => _dbContainer.GetConnectionString();
    public string InfluxDbConnectionString => _influxDbContainer.GetConnectionString();
    public int MqttPort => _mqttContainer.GetMappedPublicPort(MqttContainer.MqttPort);
    public string MqttHost => _mqttContainer.Hostname;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add configuration for load testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:UserName"] = "loadtestadmin",
                ["Admin:Email"] = "loadtestadmin@loadtest.com",
                ["Admin:Password"] = "LoadTestAdmin123!",
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                ["InfluxDB:Url"] = _influxDbContainer.GetConnectionString(),
                ["InfluxDB:Token"] = "loadtest-token-12345678901234567890",
                ["InfluxDB:Organization"] = "loadtest-org",
                ["InfluxDB:Bucket"] = "loadtest-bucket",
                ["MQTT:Server"] = _mqttContainer.Hostname,
                ["MQTT:Port"] = _mqttContainer.GetMappedPublicPort(MqttContainer.MqttPort).ToString(),
                ["MQTT:Username"] = "",
                ["MQTT:Password"] = ""
            });

            config.AddJsonFile("appsettings.loadtest.json", optional: true);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Configure logging to reduce noise during load testing
            services.Configure<LoggerFilterOptions>(options => 
            { 
                options.MinLevel = LogLevel.Error; 
            });

            // Register required services for load testing
            services.AddMemoryCache();
            services.AddScoped<CachedInfluxRepo>();
            services.AddScoped<IInfluxRepo>(provider => provider.GetRequiredService<CachedInfluxRepo>());
            services.AddScoped<ISettingsRepo, SettingsRepo>();
            services.AddSingleton<IReceiver, Receiver>();
            services.AddSingleton<IConnection, Connection>();
            services.AddScoped<ICoordinateRepo, CoordinateRepo>();
            services.AddHttpClient();
        });

        // Disable HTTPS redirection for load tests
        builder.UseSetting("HTTPS_REDIRECTION", "false");
        builder.UseEnvironment("LoadTesting");
    }

    public new HttpClient CreateClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Initialize all containers and seed test data
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start all containers in parallel
        var tasks = new[]
        {
            _dbContainer.StartAsync(),
            _influxDbContainer.StartAsync(),
            _mqttContainer.StartAsync()
        };

        await Task.WhenAll(tasks);

        // Initialize database
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Seed roles and admin user
        await SeedTestDataAsync(scope.ServiceProvider);
    }

    /// <summary>
    /// Seed basic test data for load testing
    /// </summary>
    private async Task SeedTestDataAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Create roles
        var roles = new[] { Roles.Admin, Roles.User };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create admin user for load testing
        const string adminEmail = "loadtestadmin@loadtest.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = "loadtestadmin",
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "LoadTestAdmin123!");
            await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        }

        // Create test user for API authentication
        const string testUserEmail = "loadtest@example.com";
        var testUser = await userManager.FindByEmailAsync(testUserEmail);
        if (testUser == null)
        {
            testUser = new IdentityUser
            {
                UserName = "loadtestuser",
                Email = testUserEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(testUser, "LoadTest123!");
            await userManager.AddToRoleAsync(testUser, Roles.User);
        }
    }

    /// <summary>
    /// Clean up containers
    /// </summary>
    public async Task CleanupAsync()
    {
        var tasks = new List<Task>();

        if (_dbContainer != null)
        {
            tasks.Add(_dbContainer.StopAsync());
        }

        if (_influxDbContainer != null)
        {
            tasks.Add(_influxDbContainer.StopAsync());
        }

        if (_mqttContainer != null)
        {
            tasks.Add(_mqttContainer.StopAsync());
        }

        await Task.WhenAll(tasks);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                CleanupAsync().GetAwaiter().GetResult();
                
                _dbContainer?.DisposeAsync().GetAwaiter().GetResult();
                _influxDbContainer?.DisposeAsync().GetAwaiter().GetResult();
                _mqttContainer?.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (ObjectDisposedException)
            {
                // Containers already disposed, ignore
            }
        }

        base.Dispose(disposing);
    }
}