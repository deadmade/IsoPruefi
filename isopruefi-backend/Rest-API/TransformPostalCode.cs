using System.Globalization;
using System.Text.Json;

namespace Rest_API;

public class TransformPostalCode
{
    private readonly ILogger<TransformPostalCode> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly string _geocodingApi = "https://nominatim.openstreetmap.org/search?format=jsonv2&postalcode=";
    
    public TransformPostalCode(ILogger<TransformPostalCode> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<Tuple<double, double>?> getCoordinates(double postalCode)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        // Creating a user agent for accessing the API.
        string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36";
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        

        // Getting the coordinates from nominatim.
        var response = await httpClient.GetAsync(_geocodingApi + postalCode);
        
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        
        // Getting the coordinates from the JSON file.
        var root = json.RootElement;
        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var rootElement = root[0];
            if (rootElement.TryGetProperty("lat", out var lat) &&
                rootElement.TryGetProperty("lon", out var lon))
            {
                var latDouble = double.Parse(lat.GetString(), CultureInfo.InvariantCulture);
                var lonDouble = double.Parse(lon.GetString(), CultureInfo.InvariantCulture);
                
                var coordinates = new Tuple<double, double>(latDouble, lonDouble);
                _logger.LogInformation("Coordinates retrieved successfully");
                return coordinates;
            }
            else
            {
                _logger.LogError("Coordinates could not be retrieved.");
            }
        }

        return null;
    }
}