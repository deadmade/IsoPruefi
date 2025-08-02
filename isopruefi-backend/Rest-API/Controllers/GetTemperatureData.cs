using Asp.Versioning;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest_API.Models;

namespace Rest_API.Controllers;

/// <summary>
/// Provides endpoints for retrieving temperature data from multiple sources.
/// Combines indoor sensor data with external weather data for comprehensive temperature monitoring.
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
public class TemperatureDataController : ControllerBase
{
    private readonly ILogger<TemperatureDataController> _logger;
    private ISettingsRepo _settingsRepo;
    private IInfluxRepo _influxRepo;


    /// <summary>
    /// Initializes a new instance of the <see cref="TemperatureDataController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used for logging operations.</param>
    /// <param name="settingsRepo">The repository for accessing application settings.</param>
    /// <param name="influxRepo">The repository for accessing temperature data from InfluxDB.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
    public TemperatureDataController(ILogger<TemperatureDataController> logger, ISettingsRepo settingsRepo,
        IInfluxRepo influxRepo)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
        _influxRepo = influxRepo ?? throw new ArgumentNullException(nameof(influxRepo));
    }

    /// <summary>
    /// Converts a temperature from Celsius to Fahrenheit.
    /// </summary>
    /// <param name="celsius">Temperature in Celsius.</param>
    /// <returns>Temperature in Fahrenheit.</returns>
    private static double ConvertToFahrenheit(double celsius)
    {
        return celsius * 9 / 5 + 32;
    }

    /// <summary>
    /// Retrieves comprehensive temperature data for a specified time range and location.
    /// </summary>
    /// <remarks>
    /// This endpoint provides temperature readings from multiple sources:
    /// - **Indoor sensors**: North and South sensor locations
    /// - **External weather data**: Outside temperature for the specified location
    /// 
    /// **Authorization Required**: Bearer token with User or Admin role
    /// 
    /// **Time Range Requirements**:
    /// - Start date must be before end date
    /// - Maximum time range is recommended to be 30 days for optimal performance
    /// - Dates should be in ISO 8601 format (e.g., "2024-01-15T10:30:00Z")
    /// 
    /// **Temperature Unit Conversion**:
    /// - Default: Celsius (°C)
    /// - Optional: Fahrenheit (°F) by setting `isFahrenheit=true`
    /// 
    /// **Example Usage**:
    /// ```
    /// GET /api/v1/TemperatureData/GetTemperature?start=2024-01-15T00:00:00Z&amp;end=2024-01-16T00:00:00Z&amp;place=Berlin&amp;isFahrenheit=false
    /// ```
    /// 
    /// **Data Quality**:
    /// - Automatic plausibility checks are performed on all temperature readings
    /// - Suspicious readings (outside -30°C to 45°C for outdoor, -10°C to 35°C for indoor) are logged as warnings
    /// - Large temperature jumps (>10°C between consecutive readings) are flagged
    /// </remarks>
    /// <param name="start">Start date and time for the data range (ISO 8601 format).</param>
    /// <param name="end">End date and time for the data range (ISO 8601 format).</param>
    /// <param name="place">Location name for external weather data (e.g., "Berlin", "Munich").</param>
    /// <param name="isFahrenheit">Optional. If true, converts all temperatures to Fahrenheit. Default is false (Celsius).</param>
    /// <returns>Comprehensive temperature data overview containing indoor and outdoor readings.</returns>
    /// <response code="200">Successfully retrieved temperature data. Returns comprehensive temperature overview.</response>
    /// <response code="400">Invalid parameters. Check date format, ensure start is before end date, or verify location name.</response>
    /// <response code="401">Authentication required. No valid JWT token provided.</response>
    /// <response code="403">Access denied. User or Admin role required.</response>
    /// <response code="500">Internal server error. Possible issues with database connection or external weather service.</response>
    [HttpGet]
    [ProducesResponseType(typeof(TemperatureDataOverview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> GetTemperature([FromQuery] DateTime start, [FromQuery] DateTime end,
        [FromQuery] string place, [FromQuery] bool isFahrenheit = false)
    {
        var temperatureData = await CombineTempData(start, end, place, isFahrenheit);
        return Ok(temperatureData);
    }

    /// <summary>
    /// Combines outside and sensor temperature data, applying Fahrenheit conversion if requested.
    /// </summary>
    /// <param name="start">Start date and time for the data range.</param>
    /// <param name="end">End date and time for the data range.</param>
    /// <param name="place">Location for outside temperature data.</param>
    /// <param name="isFahrenheit">If true, converts temperatures to Fahrenheit.</param>
    /// <returns>Overview of temperature data.</returns>
    private async Task<TemperatureDataOverview> CombineTempData(DateTime start, DateTime end, string place,
        bool isFahrenheit)
    {
        var outsideWeatherData = await GetOutsideTemperatureDataAsync(start, end, place);
        var sensorWeatherData = await GetSensorTemperatureDataAsync(start, end);
        var settings = await _settingsRepo.GetTopicSettingsAsync();
        var sensorNord = settings.FirstOrDefault(x => x.SensorLocation == "North");
        var sensorSouth = settings.FirstOrDefault(x => x.SensorLocation == "South");

        var tempConverter = isFahrenheit ? ConvertToFahrenheit : (Func<double, double>)(c => c);

        var temperatureData = new TemperatureDataOverview
        {
            TemperatureNord = sensorWeatherData.Where(x => x.Item3 == sensorNord?.SensorName)
                .Select(x => new TemperatureData { Timestamp = x.Item2, Temperature = tempConverter(x.Item1) })
                .OrderBy(d => d.Timestamp)
                .ToList(),

            TemperatureSouth = sensorWeatherData.Where(x => x.Item3 == sensorSouth?.SensorName)
                .Select(x => new TemperatureData { Timestamp = x.Item2, Temperature = tempConverter(x.Item1) })
                .OrderBy(d => d.Timestamp)
                .ToList(),

            TemperatureOutside = outsideWeatherData
                .Select(x => new TemperatureData { Timestamp = x.Item2, Temperature = tempConverter(x.Item1) })
                .OrderBy(d => d.Timestamp)
                .ToList()
        };

        // Testing plausibility of the deviation between consecutive temperature data.
        for (var i = 0; i < temperatureData.TemperatureNord.Count - 1; i++)
        {
            var northDeviation = temperatureData.TemperatureNord[i].Temperature -
                                 temperatureData.TemperatureNord[i + 1].Temperature;
            if (northDeviation > 10.0)
                _logger.LogWarning(
                    "Inside(North) temperature data may be corrupted, the temperature deviation has exceeded boundary values.");
        }

        for (var i = 0; i < temperatureData.TemperatureSouth.Count - 1; i++)
        {
            var southDeviation = temperatureData.TemperatureSouth[i].Temperature -
                                 temperatureData.TemperatureSouth[i + 1].Temperature;
            if (southDeviation > 10.0)
                _logger.LogWarning(
                    "Inside(south) temperature data may be corrupted, the temperature deviation has exceeded boundary values.");
        }

        for (var i = 0; i < temperatureData.TemperatureOutside.Count - 1; i++)
        {
            var outsideDeviation = temperatureData.TemperatureOutside[i].Temperature -
                                   temperatureData.TemperatureOutside[i + 1].Temperature;
            if (outsideDeviation > 10.0)
                _logger.LogWarning(
                    "Outside temperature data may be corrupted, the temperature deviation has exceeded boundary values.");
        }

        return temperatureData;
    }

    /// <summary>
    /// Retrieves outside temperature data from the InfluxDB for the specified time range and location.
    /// </summary>
    /// <param name="start">Start date and time for the data range.</param>
    /// <param name="end">End date and time for the data range.</param>
    /// <param name="place">Location for outside temperature data.</param>
    /// <returns>List of temperature and timestamp tuples.</returns>
    private async Task<List<Tuple<double, DateTime>>> GetOutsideTemperatureDataAsync(DateTime start, DateTime end,
        string place)
    {
        var temperatureData = new List<Tuple<double, DateTime>>();
        try
        {
            await foreach (var row in _influxRepo.GetOutsideWeatherData(start, end, place))
            {
                var temperature = row.GetDoubleField("value");
                var timestamp = row.GetTimestamp();

                if (timestamp == null || temperature == null || place == null)
                {
                    _logger.LogWarning(
                        "Received incomplete data from InfluxDB: Timestamp: {Timestamp}, Value: {Value}, Place: {Place}",
                        timestamp, temperature, place);
                    continue;
                }

                // Testing for plausibility of the temperature with boundary values.
                if (temperature > 45.0 || temperature < -30.0)
                    _logger.LogWarning(
                        "Outside temperature may be corrupted, the temperature has exceeded boundary values.");

                long.TryParse(timestamp.ToString(), out var timestampLong);
                timestampLong /= 1000000000;

                var datetime = DateTimeOffset.FromUnixTimeSeconds(timestampLong).UtcDateTime;
                temperatureData.Add(new Tuple<double, DateTime>(temperature ?? 0, datetime));

                _logger.LogInformation(
                    "Fetched outside temperature data: Place: {Place}, Website: {Website}, Temperature: {Temperature}",
                    place, datetime, temperature);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while fetching temperature data from InfluxDB");
        }

        return temperatureData;
    }

    /// <summary>
    /// Retrieves sensor temperature data from the InfluxDB for the specified time range
    /// </summary>
    /// <param name="start">Start date and time for the data range.</param>
    /// <param name="end">End date and time for the data range.</param>
    /// <returns>List of temperature and timestamp tuples.</returns>
    private async Task<List<Tuple<double, DateTime, string>>> GetSensorTemperatureDataAsync(DateTime start,
        DateTime end)
    {
        var temperatureData = new List<Tuple<double, DateTime, string>>();
        try
        {
            await foreach (var row in _influxRepo.GetSensorWeatherData(start, end))
            {
                var temperature = row.GetDoubleField("value");
                var sensor = row.GetTag("sensor");
                var timestamp = row.GetTimestamp();

                if (timestamp == null || temperature == null || sensor == null)
                {
                    _logger.LogWarning(
                        "Received incomplete data from InfluxDB: Timestamp: {Timestamp}, Value: {Value}",
                        timestamp, temperature);
                    continue;
                }

                // Testing for plausibility of the temperature with boundary values.
                if (temperature > 35.0 || temperature < -10.0)
                    _logger.LogWarning(
                        "Inside temperature may be corrupted, the temperature has exceeded boundary values.");

                long.TryParse(timestamp.ToString(), out var timestampLong);
                timestampLong /= 1000000000;

                var datetime = DateTimeOffset.FromUnixTimeSeconds(timestampLong).UtcDateTime;
                temperatureData.Add(new Tuple<double, DateTime, string>(temperature ?? 0, datetime, sensor));

                _logger.LogInformation(
                    "Fetched outside temperature Timestamp: {Timestamp}, Temperature: {Temperature}",
                    datetime, temperature);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while fetching temperature data from InfluxDB");
        }

        return temperatureData;
    }
}