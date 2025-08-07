using System.Text.Json;
using Database.EntityFramework.Models;
using Database.Repository.CoordinateRepo;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;

namespace Get_weatherData_worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    private readonly string _weatherDataApi;
    private readonly string _alternativeWeatherDataApi;

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory,
        IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _configuration = configuration;

        _weatherDataApi = _configuration["Weather:OpenMeteoApiUrl"] ?? throw new InvalidOperationException(
            "Weather:OpenMeteoApiUrl configuration is missing");

        _alternativeWeatherDataApi = _configuration["Weather:BrightSkyApiUrl"] ?? throw new InvalidOperationException(
            "Weather:BrightSkyApiUrl configuration is missing");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var coordinateRepo = scope.ServiceProvider.GetRequiredService<ICoordinateRepo>();
            var influxRepo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();
            
            // Getting the location information for the next unlocked entry.
            var availableLocations = await GetAvailableCoordinateMapping(coordinateRepo);
            
            if (availableLocations == null)
            {
                _logger.LogInformation("All locations have up to date weather data.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                continue;
            }

            var lat = availableLocations.Latitude; 
            var lon = availableLocations.Longitude;
            var location = availableLocations.Location;
                
            // Sending GET-Request to Meteo.
            var response = await CallMeteoApi(lat, lon);
                
            if (response != null)
            {
                // Saving the temperature in the database.
                try
                {
                    await influxRepo.WriteOutsideWeatherData(location, "Meteo", response.Temperature,
                        response.Timestamp, availableLocations.PostalCode);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Outside Weather data could not be saved in the database.");
                }
            }
            else
            {
                // Sending GET-Request to Bright Sky.
                var alternativeResponse = await CallBrightSkyApi(lat, lon);

                if (alternativeResponse != null)
                {
                    // Saving the temperature in the database.
                    try
                    {
                        await influxRepo.WriteOutsideWeatherData(location, "Bright Sky",
                            alternativeResponse.Temperature, alternativeResponse.Timestamp, availableLocations.PostalCode);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Outside Weather data could not be saved in the database.");
                    }
                }
                else
                {
                    _logger.LogError("Failed to retrieve data from both sources.");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task<CoordinateMapping?> GetAvailableCoordinateMapping(ICoordinateRepo coordinateRepo)
    {
        try
        {
            var result = await coordinateRepo.GetUnlockedLocation();
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unlocked entries could not be retrieved from the database.");
        }

        return null;
    }

    private async Task<WeatherData?> CallMeteoApi(double lat, double lon)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var weatherDataApi = _weatherDataApi
            .Replace("{lat}", lat.ToString())
            .Replace("{lon}", lon.ToString());

        var response = await httpClient.GetAsync(weatherDataApi);

        if (response.IsSuccessStatusCode)
        {
            // Getting temperature data from the Meteo response.
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
            var root = json.RootElement;
            if (root.TryGetProperty("current", out var current))
            {
                // Getting the time and temperature data from the JSON file.
                if (current.TryGetProperty("time", out var time) &&
                    current.TryGetProperty("temperature_2m", out var temperature))
                {
                    var weatherData = new WeatherData();
                    weatherData.Timestamp = time.GetDateTime();
                    weatherData.Temperature = temperature.GetDouble();

                    _logger.LogInformation("Weather data from Meteo retrieved successfully.");

                    return weatherData;
                }
                else
                {
                    _logger.LogWarning("Weather data from Meteo incomplete");
                }
            }
        }
        else
        {
            _logger.LogWarning("HTTP Request to Meteo failed with status code: " + response.StatusCode);
        }
        
        return null;
    }

    private async Task<WeatherData?> CallBrightSkyApi(double lat, double lon)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        var weatherDataApi = _alternativeWeatherDataApi
            .Replace("{lat}", lat.ToString())
            .Replace("{lon}", lon.ToString());
        
        var response = await httpClient.GetAsync(weatherDataApi);

        if (response.IsSuccessStatusCode)
        {
            // Getting temperature data from the Bright Sky response.
            using var alternativeJson =
                JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
            var root = alternativeJson.RootElement;
            if (root.TryGetProperty("weather", out var weather))
            {
                // Getting the time and temperature data from the JSON file.
                if (weather.TryGetProperty("timestamp", out var time) &&
                    weather.TryGetProperty("temperature", out var temperature))
                {
                    var weatherData = new WeatherData();
                    weatherData.Timestamp = time.GetDateTime();
                    weatherData.Temperature = temperature.GetDouble();

                    _logger.LogInformation("Weather data from Bright Sky retrieved successfully.");
                    return weatherData;
                }
                else
                {
                    _logger.LogWarning("Data from Bright Sky incomplete.");
                }
            }
        }
        else
        {
            _logger.LogWarning("HTTP Request to Bright Sky failed with status code: " + response.StatusCode);
        }
        
        return null;
    }
}