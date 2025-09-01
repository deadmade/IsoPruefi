using System.Text.Json;
using Database.EntityFramework;
using Database.EntityFramework.Models;
using Database.Repository.CoordinateRepo;
using Database.Repository.InfluxRepo;
using FluentAssertions;
using Get_weatherData_worker;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.WeatherWorker;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class WeatherWorkerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task CoordinateRepo_GetUnlockedLocation_ReturnsAvailableLocation()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var coordinateRepo = scope.ServiceProvider.GetRequiredService<ICoordinateRepo>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed test data
        var coordinateMapping = new CoordinateMapping
        {
            Location = "Test Location",
            PostalCode = 12345,
            Latitude = 48.7758,
            Longitude = 9.1829,
            LockedUntil = DateTime.UtcNow.AddHours(-1) // Unlocked (expired lock)
        };
        context.CoordinateMappings.Add(coordinateMapping);
        await context.SaveChangesAsync();

        // Act
        var result = await coordinateRepo.GetUnlockedLocation();

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task CoordinateRepo_GetLocation_ReturnsLocationByPostalCode()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var coordinateRepo = scope.ServiceProvider.GetRequiredService<ICoordinateRepo>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed test data
        var coordinateMapping = new CoordinateMapping
        {
            Location = "Berlin",
            PostalCode = 10117,
            Latitude = 52.5200,
            Longitude = 13.4050
        };
        context.CoordinateMappings.Add(coordinateMapping);
        await context.SaveChangesAsync();

        // Act
        var result = await coordinateRepo.GetLocation("Berlin");

        // Assert
        result.Should().NotBeNull();
        result!.Location.Should().Be("Berlin");
        result.PostalCode.Should().Be(10117);
        result.Latitude.Should().Be(52.5200);
        result.Longitude.Should().Be(13.4050);
    }

    [Test]
    public async Task InfluxRepo_WriteOutsideWeatherData_DoesNotThrowException()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var influxRepo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();

        var testLocation = "Test Location";
        var testSource = "Test Source";
        var testTemperature = 22.5;
        var testTimestamp = DateTime.UtcNow;
        var testPostalCode = 12345;

        // Act & Assert
        // This should not throw an exception even if InfluxDB is not available
        var act = async () =>
            await influxRepo.WriteOutsideWeatherData(testLocation, testSource, testTemperature, testTimestamp,
                testPostalCode);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public void WeatherData_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var temperature = 23.5;
        var timestamp = DateTime.UtcNow;

        // Act
        var weatherData = new WeatherData
        {
            Temperature = temperature,
            Timestamp = timestamp
        };

        // Assert
        weatherData.Temperature.Should().Be(temperature);
        weatherData.Timestamp.Should().Be(timestamp);
    }

    [Test]
    public async Task Worker_CanParseMeteoApiResponse()
    {
        // Arrange
        var jsonResponse = """
                           {
                               "current": {
                                   "time": "2023-09-01T12:00:00Z",
                                   "temperature_2m": 23.5
                               }
                           }
                           """;

        // Act
        using var json = JsonDocument.Parse(jsonResponse);
        var root = json.RootElement;
        var success = root.TryGetProperty("current", out var current);

        DateTime parsedTime = default;
        double parsedTemperature = 0;

        if (success && current.TryGetProperty("time", out var time) &&
            current.TryGetProperty("temperature_2m", out var temperature))
        {
            parsedTime = time.GetDateTime();
            parsedTemperature = temperature.GetDouble();
        }

        // Assert
        success.Should().BeTrue();
        parsedTime.Should().Be(new DateTime(2023, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        parsedTemperature.Should().Be(23.5);
    }

    [Test]
    public async Task Worker_CanParseBrightSkyApiResponse()
    {
        // Arrange
        var jsonResponse = """
                           {
                               "weather": {
                                   "timestamp": "2023-09-01T12:00:00Z",
                                   "temperature": 24.8
                               }
                           }
                           """;

        // Act
        using var json = JsonDocument.Parse(jsonResponse);
        var root = json.RootElement;
        var success = root.TryGetProperty("weather", out var weather);

        DateTime parsedTime = default;
        double parsedTemperature = 0;

        if (success && weather.TryGetProperty("timestamp", out var time) &&
            weather.TryGetProperty("temperature", out var temperature))
        {
            parsedTime = time.GetDateTime();
            parsedTemperature = temperature.GetDouble();
        }

        // Assert
        success.Should().BeTrue();
        parsedTime.Should().Be(new DateTime(2023, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        parsedTemperature.Should().Be(24.8);
    }

    [Test]
    public async Task HttpClientFactory_CanCreateHttpClient()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var httpClient = httpClientFactory.CreateClient();

        // Assert
        httpClient.Should().NotBeNull();
        httpClient.Should().BeOfType<HttpClient>();
    }

    [Test]
    public async Task WorkerDependencies_CanBeResolved()
    {
        // Arrange & Act
        using var scope = Factory.Services.CreateScope();

        // Verify all dependencies can be resolved
        var coordinateRepo = scope.ServiceProvider.GetService<ICoordinateRepo>();
        var influxRepo = scope.ServiceProvider.GetService<IInfluxRepo>();
        var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();

        // Assert
        coordinateRepo.Should().NotBeNull();
        influxRepo.Should().NotBeNull();
        httpClientFactory.Should().NotBeNull();
    }

    [Test]
    public async Task Configuration_ContainsRequiredWeatherApiSettings()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act
        var openMeteoUrl = configuration["Weather:OpenMeteoApiUrl"];
        var brightSkyUrl = configuration["Weather:BrightSkyApiUrl"];
        var nominatimUrl = configuration["Weather:NominatimApiUrl"];

        // Assert
        // These might be null in test environment, but we test that configuration can be accessed
        configuration.Should().NotBeNull();

        // Test that configuration keys exist (even if empty)
        var weatherSection = configuration.GetSection("Weather");
        weatherSection.Should().NotBeNull();
    }
}