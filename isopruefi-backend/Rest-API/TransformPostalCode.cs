using Database.EntityFramework.Models;
using Database.Repository.SettingsRepo;
using System.Globalization;
using System.Text.Json;

namespace Rest_API;

public class TransformPostalCode
{
    private readonly ILogger<TransformPostalCode> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsRepo _settingsRepo;
    private readonly IConfiguration _configuration;

    private readonly string _geocodingApi;
    
    public TransformPostalCode(ILogger<TransformPostalCode> logger, IHttpClientFactory httpClientFactory, ISettingsRepo settingsRepo, IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _settingsRepo = settingsRepo;
        _configuration = configuration;

        //_geocodingApi = _configuration["Weather:NominatimApiUrl"] ?? throw new InvalidOperationException(
            //"Weather:NominatimApiUrl configuration is missing");

            _geocodingApi = "https://nominatim.openstreetmap.org/search?format=jsonv2&postalcode=";
    }
    
    public async Task GetCoordinates(int postalCode)
    {
        // Checking if there is an entry for that location in the database.
        var existingEntry = await _settingsRepo.ExistsPostalCode(postalCode);
        if (existingEntry)
        {
            // If an entry exists the time for that entry is updated.
            var newTime = DateTime.UtcNow;
            try
            {
                await _settingsRepo.UpdateTime(postalCode, newTime);
                _logger.LogInformation("Time for location updated succesfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while updating the time for the location.");
            }
        }
        else
        {
            // If there is no entry an API will be used to get the coordinates.
            var httpClient = _httpClientFactory.CreateClient();

            // Creating a user agent for accessing the API.
            string userAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36";
            httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

            
            try
            {
                // Getting the coordinates from nominatim.
                var response = await httpClient.GetAsync(_geocodingApi + postalCode);

                if (response.IsSuccessStatusCode)
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

                    // Getting the coordinates from the JSON file.
                    var root = json.RootElement;
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var rootElement = root[0];
                        if (rootElement.TryGetProperty("lat", out var lat) &&
                            rootElement.TryGetProperty("lon", out var lon) &&
                            rootElement.TryGetProperty("display_name", out var location))
                        {
                            var latDouble = double.Parse(lat.GetString(), CultureInfo.InvariantCulture);
                            var lonDouble = double.Parse(lon.GetString(), CultureInfo.InvariantCulture);
                            var locationString = location.GetString();
                            var splitLocation = locationString.Split(",");
                            string locationName = splitLocation[1];
                            
                            var postalCodeLocation = new CoordinateMapping
                            {
                                PostalCode = postalCode,
                                Location = locationName,
                                Latitude = latDouble,
                                Longitude = lonDouble,
                                LastUsed = DateTime.UtcNow
                            };

                            // Saving the new location in the database.
                            try
                            {
                                await _settingsRepo.InsertNewPostalCode(postalCodeLocation);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Exception while saving new location.");
                            }
                            
                            _logger.LogInformation("Coordinates retrieved successfully");
                        }
                        else
                        {
                            _logger.LogError("Coordinates and city name could not be retrieved.");
                        }
                    }
                }
                else
                {
                    _logger.LogError("Getting coordinates failed with HTTP status code: " + response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while calling geocoding API.");
            }
        }
    }
}