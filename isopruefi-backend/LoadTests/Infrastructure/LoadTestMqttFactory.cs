using Database.EntityFramework;
using Database.Repository.InfluxRepo;
using Database.Repository.InfluxRepo.InfluxCache;
using Database.Repository.SettingsRepo;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTT_Receiver_Worker;
using MQTT_Receiver_Worker.MQTT;
using MQTT_Receiver_Worker.MQTT.Interfaces;

namespace LoadTests.Infrastructure;

/// <summary>
///     Web application factory for MQTT load tests using TestContainers
/// </summary>
public class LoadTestMqttFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _influxDbHost;
    private readonly string _influxDbToken;
    private readonly IContainer _mosquittoContainer;

    /// <summary>
    ///     Initializes a new instance of the LoadTestMqttFactory
    /// </summary>
    /// <param name="dbConnectionString">Database connection string</param>
    /// <param name="influxDbToken">InfluxDB authentication token</param>
    /// <param name="influxDbHost">InfluxDB host URL</param>
    public LoadTestMqttFactory(string dbConnectionString, string influxDbToken, string influxDbHost)
    {
        _mosquittoContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto")
            .WithPortBinding(1883, true)
            .WithPortBinding(9001, true)
            .WithBindMount(CreateMosquittoConfigFile(), "/mosquitto/config/mosquitto.conf")
            .WithVolumeMount("mosquitto", "/etc/mosquitto")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(1883))
            .WithCleanUp(true)
            .Build();

        _connectionString = dbConnectionString;
        _influxDbToken = influxDbToken;
        _influxDbHost = influxDbHost;
    }

    /// <summary>
    ///     Gets the mapped MQTT broker port for external connections
    /// </summary>
    public int MqttPort => _mosquittoContainer.GetMappedPublicPort(1883);

    /// <summary>
    ///     Creates a temporary Mosquitto configuration file for load testing
    /// </summary>
    /// <returns>Path to the created configuration file</returns>
    private string CreateMosquittoConfigFile()
    {
        var configPath = Path.Combine(Path.GetTempPath(), "mosquitto_loadtest.conf");
        var configContent = @"listener 1883
    allow_anonymous true
    ";

        File.WriteAllText(configPath, configContent);
        return configPath;
    }

    /// <summary>
    ///     Configures the web host for MQTT load testing
    /// </summary>
    /// <param name="builder">Web host builder to configure</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add configuration for load testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MQTT:BrokerHost"] = "localhost",
                ["MQTT:BrokerPort"] = MqttPort.ToString(),
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Influx:InfluxDBHost"] = _influxDbHost,
                ["Influx:InfluxDBToken"] = _influxDbToken,
                ["DOTNET_ENVIRONMENT"] = "Docker"
            });
        });

        builder.UseEnvironment("Docker");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options => { options.UseNpgsql(_connectionString); });

            // Configure logging to reduce noise during load testing
            services.Configure<LoggerFilterOptions>(options => { options.MinLevel = LogLevel.Information; });

            // Register required services for load testing
            services.AddMemoryCache();
            services.AddScoped<CachedInfluxRepo>();
            services.AddScoped<IInfluxRepo>(provider => provider.GetRequiredService<CachedInfluxRepo>());
            services.AddScoped<ISettingsRepo, SettingsRepo>();
            services.AddSingleton<IReceiver, Receiver>();
            services.AddSingleton<IConnection, Connection>();

            services.AddHostedService<Worker>();
            services.AddHostedService<InfluxRetryService>();
        });

        // Disable HTTPS redirection for load tests
        builder.UseSetting("HTTPS_REDIRECTION", "false");
        builder.UseEnvironment("LoadTesting");
    }

    /// <summary>
    ///     Initialize all containers and seed test data
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start all containers in parallel
        var tasks = new[]
        {
            _mosquittoContainer.StartAsync()
        };

        await Task.WhenAll(tasks);

        using var scope = Services.CreateScope();
        ApplicationDbContext.ApplyMigration<ApplicationDbContext>(scope);
    }

    /// <summary>
    ///     Clean up containers
    /// </summary>
    public async Task CleanupAsync()
    {
        var tasks = new List<Task>();

        try
        {
            if (_mosquittoContainer != null) tasks.Add(_mosquittoContainer.StopAsync());

            await Task.WhenAll(tasks);
        }
        catch (ObjectDisposedException)
        {
            // Containers already disposed, ignore
        }
    }

    /// <summary>
    ///     Disposes of container resources
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            try
            {
                CleanupAsync().GetAwaiter().GetResult();

                _mosquittoContainer?.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (ObjectDisposedException)
            {
                // Containers already disposed, ignore
            }

        base.Dispose(disposing);
    }
}