using Database.EntityFramework.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Database.Repository.CoordinateRepo;

namespace Rest_API.Services.Temp;

/// <summary>
/// Provides operations related to the location for the outside temperature data, for example getting the right coordinates for the postalcode.
/// </summary>
public class TempService : ITempService
{
    private readonly ILogger<TempService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICoordinateRepo _coordinateRepo;
    private readonly IConfiguration _configuration;

    private readonly string _geocodingApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging actions and errors.</param>
    /// <param name="httpClientFactory">The httpClient for API calls.</param>
    /// <param name="coordinateRepo">The settingsRepo instance for connection with the postgres database.</param>
    /// <param name="configuration"></param>
    public TempService(ILogger<TempService> logger, IHttpClientFactory httpClientFactory,
        ICoordinateRepo coordinateRepo, IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _coordinateRepo = coordinateRepo;
        _configuration = configuration;

        _geocodingApi = _configuration["Weather:NominatimApiUrl"] ?? throw new InvalidOperationException(
            "Weather:NominatimApiUrl configuration is missing");
    }

    /// <inheritdoc />
    public async Task GetCoordinates(int postalCode)
    {
        // Checking if there is an entry for that location in the database.
        var existingEntry = await _coordinateRepo.ExistsPostalCode(postalCode);
        if (existingEntry)
        {
            _logger.LogInformation("There is an existing entry for that postalcode");
        }
        else
        {
            var response = await GetCoordinatesApi(postalCode);
            if (response == null)
            {
                _logger.LogError("No response received from API");
                return;
            }

            try
            {
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
                            var locationName = splitLocation[1];

                            var postalCodeLocation = new CoordinateMapping
                            {
                                PostalCode = postalCode,
                                Location = locationName,
                                Latitude = latDouble,
                                Longitude = lonDouble
                            };

                            // Saving the new location in the database.
                            try
                            {
                                await _coordinateRepo.InsertNewPostalCode(postalCodeLocation);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Exception while saving new location");
                            }

                            _logger.LogInformation("Coordinates retrieved successfully");
                        }
                        else
                        {
                            _logger.LogError("Coordinates and city name could not be retrieved");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("The plz does not exist or is invalid.");
                    }
                }
                else
                {
                    _logger.LogError("Getting coordinates failed with HTTP status code: " + response.StatusCode);
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                        throw new InvalidOperationException("The limit is exceeded, please try again later.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while calling geocoding API");
                throw;
            }
        }
    }

    private async Task<HttpResponseMessage?> GetCoordinatesApi(int postalCode)
    {
        // If there is no entry an API will be used to get the coordinates.
        var httpClient = _httpClientFactory.CreateClient();

        // Creating a user agent for accessing the API.
        var userAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36";
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

        try
        {
            // Getting the coordinates from nominatim.
            var response = await httpClient.GetAsync(_geocodingApi + postalCode);
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Calling the API was not successful");
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<List<Tuple<int, string>>?> ShowAvailableLocations()
    {
        try
        {
            var allPostalcodes = await _coordinateRepo.GetAllLocations();
            return allPostalcodes;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while fetching postalcodes from the database");
        }

        return null;
    }
}