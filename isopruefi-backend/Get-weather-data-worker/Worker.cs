namespace Get_weather_data_worker;

public class WeatherData
    {
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
    }

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient();

        static async Task<Product> GetWeatherDataAsync(string path)
        {
            string path_1 = "https://api.open-meteo.com/v1/forecast?latitude=48.678&longitude=10.1516&models=icon_seamless&current=temperature_2m";
            string path_2 = "http://goweather.xyz/weather/Heidenheim";
            WeatherData weatherData = new WeatherData();

            HttpResponseMessage response = await httpClient.GetAsync(path_1);

            if (response.IsSucccessStatusCode)
            {
                string json_1 = await response.Content.ReadAsStringAsync();

                using JsonDocument document = JsonDocument.Parse(json_1);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("current", out JsonElement currentElement) && currentElement.ValueKind == JsonValueKind.Array)
                {
                    weatherData.Temperature = currentElement.GetProperty("temperature_2m").GetDouble();
                }

                if (root.TryGetProperty("timestamp", out JsonElement currentElement))
                {
                    weatherData.Timestamp = DateTime.Parse(currentElement.GetString());
                }

                _logger.LogInformation("Weather data from Meteo retrieved successfully.");
            }
            else
            {
                HttpRespnseMessage response = await httpClient.GetAsync(path_2);

                if (response.IsSucccessStatusCode)
                {
                    string json_2 = await response.COntent.ReadAsStringAsync();

                    using JsonDocument document = JsonDocument.Parse(json_2);
                    JsonElement root = document.RootElement;

                    if (root.TryGetProperty("temperature", out JsonElement currentElement))
                    {
                        weatherData.Temperature = currentElement.GetDouble();
                        weatherData.Timestamp = DateTime.now;
                    }

                    _logger.LogInformation("Weather data from GoWeather retrieved successfully.");
                }
                else
                {
                    _loogger.LogInformation("Failed to retrieve data from both sources.");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}