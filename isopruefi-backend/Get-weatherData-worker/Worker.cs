using System.Text.Json;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Microsoft.Extensions.Configuration;

namespace Get_weatherData_worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    private readonly string _weatherDataApi;
    private readonly string _alternativeWeatherDataApi;
    private readonly string _location;

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

        _location = _configuration["Weather:Location"] ?? "Heidenheim"; // Will be changed in the future to a more dynamic solution
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        // Getting the coordinates from the database.
        double lat = 0.0;
        double lon = 0.0;
        try
        {
            var coordinates = await _settingsRepo.GetCoordinates();
            lat = coordinates.Item1;
            lon = coordinates.Item2;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve coordinates.");
        }
        
        // Setting the coordinates in the API.
        string weatherDataApi = 
            "https://api.open-meteo.com/v1/forecast?latitude=" + lat + "&longitude=" + lon + "&models=icon_seamless&current=temperature_2m";
        string alternativeWeatherDataApi =
            "https://api.brightsky.dev/current_weather?lat=" + lat + "&lon=" + lon;

        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var influxRepo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();

            var weatherData = new WeatherData();

            // Sending GET-Request to Meteo.
            var response = await httpClient.GetAsync(_weatherDataApi);

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
                        weatherData.Timestamp = time.GetDateTime();
                        weatherData.Temperature = temperature.GetDouble();

                        // Saving the temperature in the database.
                        try
                        {
                            await influxRepo.WriteOutsideWeatherData(_location, "Meteo", weatherData.Temperature,
                                weatherData.Timestamp);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Outside Weather data could not be saved in the database.");
                        }

                        _logger.LogInformation("Weather data from Meteo retrieved successfully.");
                    }
                    else
                    {
                        _logger.LogError("Data from Meteo incomplete.");
                    }
                }
                else
                {
                    _logger.LogError("Data from Meteo incomplete.");
                }
            }
            else
            {
                // Sending GET-Request to Bright Sky.
                var alternativeResponse = await httpClient.GetAsync(_alternativeWeatherDataApi);

                if (alternativeResponse.IsSuccessStatusCode)
                {
                    // Getting temperature data from the Bright Sky response.
                    using var alternativeJson =
                        JsonDocument.Parse(await alternativeResponse.Content.ReadAsStreamAsync());
                    var root = alternativeJson.RootElement;
                    if (root.TryGetProperty("weather", out var weather))
                    {
                        // Getting the time and temperature data from the JSON file.
                        if (weather.TryGetProperty("timestamp", out var time) &&
                            weather.TryGetProperty("temperature", out var temperature))
                        {
                            weatherData.Timestamp = time.GetDateTime();
                            weatherData.Temperature = temperature.GetDouble();

                            // Saving the temperature in the database.
                            try
                            {
                                await influxRepo.WriteOutsideWeatherData(_location, "Bright Sky",
                                    weatherData.Temperature,
                                    weatherData.Timestamp);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Outside Weather data could not be saved in the database.");
                            }

                            _logger.LogInformation("Weather data from Bright Sky retrieved successfully.");
                        }
                        else
                        {
                            _logger.LogError("Data from Bright Sky incomplete.");
                        }
                    }
                    else
                    {
                        _logger.LogError("Data from Bright Sky incomplete.");
                    }
                }
                else
                {
                    _logger.LogError("Failed to retrieve data from both sources.");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}