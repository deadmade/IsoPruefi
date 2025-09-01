using System.Text.RegularExpressions;
using Database.EntityFramework;
using Database.Repository.CoordinateRepo;
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
using MQTT_Receiver_Worker.MQTT;
using MQTT_Receiver_Worker.MQTT.Interfaces;
using Rest_API;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace LoadTests.Infrastructure;

/// <summary>
///     Web application factory for load tests using TestContainers
/// </summary>
public class LoadTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly IContainer _influxDbContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;

    public LoadTestWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:alpine3.21")
            .WithDatabase("isopruefi_loadtest")
            .WithUsername("loadtest")
            .WithPassword("LoadTest123!")
            .Build();

        _influxDbContainer = new ContainerBuilder()
            .WithImage("influxdb:3.2.1-core")
            .WithPortBinding(8181, true)
            .WithEnvironment("INFLUXDB_DATABASE", "IsoPruefi")
            .WithVolumeMount("influxdb_data_loadtest", "/var/lib/influxdb3")
            .WithCommand("influxdb3", "serve", "--node-id=node0", "--object-store=file",
                "--data-dir=/var/lib/influxdb3")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(8181))
            .WithCleanUp(true)
            .Build();


        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.11")
            .Build();
    }

    public string DatabaseConnectionString => _dbContainer.GetConnectionString();
    public string InfluxDbUrl => $"http://localhost:{_influxDbContainer.GetMappedPublicPort(8181)}";
    public string InfluxDbToken { get; private set; } = string.Empty;

    public string MqttHost => _rabbitMqContainer.Hostname;
    public string RabbitMqConnectionString => _rabbitMqContainer.GetConnectionString();

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
                ["InfluxDB:Url"] = $"http://localhost:{_influxDbContainer.GetMappedPublicPort(8181)}",
                ["InfluxDB:Token"] = InfluxDbToken,
                ["MQTT:Server"] = _rabbitMqContainer.Hostname,
                ["MQTT:Port"] = 1883.ToString(),
                ["MQTT:Username"] = "",
                ["MQTT:Password"] = "",
                ["RabbitMQ:ConnectionString"] = _rabbitMqContainer.GetConnectionString()
            });

            config.AddJsonFile("appsettings.loadtest.json", true);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database context
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Configure logging to reduce noise during load testing
            services.Configure<LoggerFilterOptions>(options => { options.MinLevel = LogLevel.Information; });

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
    ///     Initialize all containers and seed test data
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start all containers in parallel
        var tasks = new[]
        {
            _dbContainer.StartAsync(),
            _influxDbContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        };

        await Task.WhenAll(tasks);

        // Create InfluxDB admin token
        var command = new List<string>
        {
            "influxdb3",
            "create",
            "token",
            "--admin"
        };

        var result = await _influxDbContainer.ExecAsync(command, CancellationToken.None);
        InfluxDbToken = ParseTokenFromOutput(result.Stdout);

        // Initialize database
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    private string ParseTokenFromOutput(string tokenOutput)
    {
        // Remove ANSI escape codes and split by lines
        var cleanOutput = Regex.Replace(tokenOutput, @"\x1B\[[0-9;]*[mK]", "");
        var lines = cleanOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Find the line with "Token:" and extract the token
        foreach (var line in lines)
            if (line.Contains("Token:"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length > 1) return parts[1].Trim();
            }

        return string.Empty;
    }

    /// <summary>
    ///     Clean up containers
    /// </summary>
    public async Task CleanupAsync()
    {
        var tasks = new List<Task>();

        if (_dbContainer != null) tasks.Add(_dbContainer.StopAsync());

        if (_influxDbContainer != null) tasks.Add(_influxDbContainer.StopAsync());

        if (_rabbitMqContainer != null) tasks.Add(_rabbitMqContainer.StopAsync());

        if (_rabbitMqContainer != null) tasks.Add(_rabbitMqContainer.StopAsync());

        await Task.WhenAll(tasks);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            try
            {
                CleanupAsync().GetAwaiter().GetResult();

                _dbContainer?.DisposeAsync().GetAwaiter().GetResult();
                _influxDbContainer?.DisposeAsync().GetAwaiter().GetResult();
                _rabbitMqContainer?.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (ObjectDisposedException)
            {
                // Containers already disposed, ignore
            }

        base.Dispose(disposing);
    }
}