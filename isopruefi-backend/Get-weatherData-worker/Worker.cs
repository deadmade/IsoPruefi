using System.Text.Json;
using Database.Repository.InfluxRepo;

namespace Get_weatherData_worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IInfluxRepo _influxRepo;

    private readonly string _weatherDataApi =
        "https://api.open-meteo.com/v1/forecast?latitude=48.678&longitude=10.1516&models=icon_seamless&current=temperature_2m";

    private readonly string _alternativeWeatherDataApi =
        "https://api.brightsky.dev/current_weather?lat=48.67&lon=10.1516";

    private readonly string _location = "Heidenheim";

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IInfluxRepo influxRepo)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _influxRepo = influxRepo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        while (!stoppingToken.IsCancellationRequested)
        {
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
                        await _influxRepo.WriteOutsideWeatherData(_location, "Meteo", weatherData.Temperature,
                            weatherData.Timestamp);

                        _logger.LogInformation("Weather data from Meteo retrieved successfully.");
                    }
                    else
                    {
                        _logger.LogInformation("Data from Meteo incomplete.");
                    }
                }
                else
                {
                    _logger.LogInformation("Data from Meteo incomplete.");
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
                            await _influxRepo.WriteOutsideWeatherData(_location, "Bright Sky", weatherData.Temperature,
                                weatherData.Timestamp);

                            _logger.LogInformation("Weather data from Bright Sky retrieved successfully.");
                        }
                        else
                        {
                            _logger.LogInformation("Data from Bright Sky incomplete.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Data from Bright Sky incomplete.");
                    }
                }
                else
                {
                    _logger.LogInformation("Failed to retrieve data from both sources.");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}