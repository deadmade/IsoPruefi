using System.Text.Json;
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
        double lat = 0.0;
        double lon = 0.0;
        string locationName = "";
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepo>();
            var influxRepo = scope.ServiceProvider.GetRequiredService<IInfluxRepo>();
            var weatherData = new WeatherData();
            
            // Getting the coordinates from the database.
            try
            {
                var location = await settingsRepo.GetLocation();
                lat = location.Item2;
                lon = location.Item3;
                locationName = location.Item1;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to retrieve coordinates.");
            }

            // Sending GET-Request to Meteo.
            var response = await callMeteoApi(lat, lon);

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
                            await influxRepo.WriteOutsideWeatherData(locationName, "Meteo", weatherData.Temperature,
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
                var alternativeResponse = await callBrightSkyApi(lat, lon);

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
                                await influxRepo.WriteOutsideWeatherData(locationName, "Bright Sky",
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

    private async Task<HttpResponseMessage> callMeteoApi(double lat, double lon)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        var weatherDataApi = _weatherDataApi
            .Replace("{lat}", lat.ToString())
            .Replace("{lon}", lon.ToString());
        
        var response = await httpClient.GetAsync(weatherDataApi);

        return response;
    }
    
    private async Task<HttpResponseMessage> callBrightSkyApi(double lat, double lon)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        var weatherDataApi = _alternativeWeatherDataApi
            .Replace("{lat}", lat.ToString())
            .Replace("{lon}", lon.ToString());
        
        var response = await httpClient.GetAsync(weatherDataApi);

        return response;
    }
}