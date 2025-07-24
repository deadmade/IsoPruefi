using System.Text.Json;
using Database.Repository.InfluxRepo;

namespace Get_weatherData_worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IInfluxRepo _influxRepo;
    
    private readonly string _weatherDataApi = "https://api.open-meteo.com/v1/forecast?latitude=48.678&longitude=10.1516&models=icon_seamless&current=temperature_2m";
    private readonly string _alternativeWeatherDataApi = "https://api.brightsky.dev/current_weather?lat=48.67&lon=10.1516";
    private readonly string _location = "Heidenheim";

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IInfluxRepo influxRepo)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _influxRepo = influxRepo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient();

        while (!stoppingToken.IsCancellationRequested)
        {
            WeatherData weatherData = new WeatherData();

            // Sending GET-Request to Meteo.
            HttpResponseMessage response = await httpClient.GetAsync(_weatherDataApi);

            if (response.IsSuccessStatusCode)
            {
                // Getting temperature data from the Meteo response.
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                JsonElement root = json.RootElement;
                JsonElement current;
                root.TryGetProperty("current", out current);

                // Getting time data from the JSON-file.
                JsonElement time;
                current.TryGetProperty("time", out time);
                weatherData.Timestamp = time.GetDateTime();

                // Getting temperature data from the JSON-file.
                JsonElement temperature;
                current.TryGetProperty("temperature_2m", out temperature);
                weatherData.Temperature = temperature.GetDouble();

                // Saving the temperature in the database.
                await _influxRepo.WriteOutsideWeatherData(_location, "Meteo", weatherData.Temperature,
                    weatherData.Timestamp);

                _logger.LogInformation("Weather data from Meteo retrieved successfully.");
            }
            else
            {
                // Sending GET-Request to Bright Sky.
                HttpResponseMessage alternativeResponse = await httpClient.GetAsync(_alternativeWeatherDataApi);

                if (alternativeResponse.IsSuccessStatusCode)
                {
                    // Getting temperature data from the Bright Sky response.
                    JsonDocument alternativeJson =
                        JsonDocument.Parse(await alternativeResponse.Content.ReadAsStreamAsync());
                    JsonElement root = alternativeJson.RootElement;
                    JsonElement weather;
                    root.TryGetProperty("weather", out weather);

                    // Getting time data from the JSON-file.
                    JsonElement time;
                    weather.TryGetProperty("timestamp", out time);
                    weatherData.Timestamp = time.GetDateTime();

                    // Getting temperature data from the JSON-file.
                    JsonElement temperature;
                    weather.TryGetProperty("temperature", out temperature);
                    weatherData.Temperature = temperature.GetDouble();

                    // Saving the temperature in the database.
                    await _influxRepo.WriteOutsideWeatherData(_location, "Bright Sky", weatherData.Temperature,
                        weatherData.Timestamp);

                    _logger.LogInformation("Weather data from GoWeather retrieved successfully.");
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