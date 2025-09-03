using Asp.Versioning;
using Database.EntityFramework.Enums;
using Database.EntityFramework.Models;
using Database.Repository.SettingsRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rest_API.Controllers;

/// <summary>
///     Manages MQTT topic configuration settings for the temperature monitoring system.
///     Provides endpoints for configuring sensor locations and MQTT topic mappings.
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
public class TopicController : ControllerBase
{
    private readonly ISettingsRepo _settingsRepo;

    /// <summary>
    ///     Initializes a new instance of the TopicController
    /// </summary>
    /// <param name="settingsRepo">Settings repository for topic operations</param>
    public TopicController(ISettingsRepo settingsRepo)
    {
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
    }

    /// <summary>
    ///     Retrieves all configured MQTT topic settings from the system.
    /// </summary>
    /// <remarks>
    ///     This endpoint returns all MQTT topic configurations including:
    ///     - Sensor names and their physical locations (North, South, etc.)
    ///     - MQTT topic mappings for each sensor
    ///     - Configuration metadata for the monitoring system
    ///     **Authorization Required**: Bearer token with Admin role
    ///     This information is essential for:
    ///     - System administration and configuration management
    ///     - Troubleshooting sensor connectivity issues
    ///     - Understanding the current sensor topology
    ///     **Example Response**:
    ///     ```json
    ///     [
    ///     {
    ///     "id": 1,
    ///     "sensorName": "TempSensor_01",
    ///     "sensorLocation": "North",
    ///     "mqttTopic": "sensors/temperature/north",
    ///     "isActive": true
    ///     },
    ///     {
    ///     "id": 2,
    ///     "sensorName": "TempSensor_02",
    ///     "sensorLocation": "South",
    ///     "mqttTopic": "sensors/temperature/south",
    ///     "isActive": true
    ///     }
    ///     ]
    ///     ```
    /// </remarks>
    /// <returns>A comprehensive list of all MQTT topic settings and sensor configurations.</returns>
    /// <response code="200">Successfully retrieved all topic settings.</response>
    /// <response code="401">Authentication required. No valid JWT token provided.</response>
    /// <response code="403">Access denied. Admin role required to view topic configurations.</response>
    /// <response code="500">Internal server error. Database connection issues or configuration service unavailable.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TopicSetting>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<TopicSetting>>> GetAllTopics()
    {
        try
        {
            var topics = await _settingsRepo.GetTopicSettingsAsync();
            return Ok(topics);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //[Authorize(Policy = "AdminOnly")]
    public ActionResult<List<string>> GetAllSensorTypes()
    {
        try
        {
            var types = Enum.GetNames(typeof(SensorType)).ToList();
            return Ok(types);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }

    /// <summary>
    ///     Creates a new MQTT topic configuration for sensor monitoring.
    /// </summary>
    /// <remarks>
    ///     This endpoint allows administrators to add new sensor configurations to the monitoring system.
    ///     Each topic setting maps a physical sensor to its MQTT topic and location within the building.
    ///     **Authorization Required**: Bearer token with Admin role
    ///     **Required Fields**:
    ///     - `sensorName`: Unique identifier for the sensor (e.g., "TempSensor_03")
    ///     - `sensorLocation`: Physical location (e.g., "North", "South", "East", "West", "Center")
    ///     - `mqttTopic`: MQTT topic path for this sensor (e.g., "sensors/temperature/east")
    ///     **Example Request**:
    ///     ```json
    ///     {
    ///     "sensorName": "TempSensor_03",
    ///     "sensorLocation": "East",
    ///     "mqttTopic": "sensors/temperature/east",
    ///     "isActive": true,
    ///     "description": "Temperature sensor in the eastern section"
    ///     }
    ///     ```
    ///     **Example Response**:
    ///     ```json
    ///     {
    ///     "id": 3,
    ///     "message": "Topic created successfully"
    ///     }
    ///     ```
    ///     **Validation Rules**:
    ///     - Sensor names must be unique across the system
    ///     - MQTT topics should follow the pattern: `sensors/temperature/{location}`
    ///     - Location names should be descriptive and consistent
    /// </remarks>
    /// <param name="topicSetting">The complete topic setting configuration to create.</param>
    /// <returns>The ID of the newly created topic setting along with a success message.</returns>
    /// <response code="201">Topic setting created successfully. Returns the new topic ID.</response>
    /// <response code="400">Invalid topic setting data, missing required fields, or duplicate sensor name.</response>
    /// <response code="401">Authentication required. No valid JWT token provided.</response>
    /// <response code="403">Access denied. Admin role required to create topic configurations.</response>
    /// <response code="500">Internal server error. Database connection issues or configuration service unavailable.</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> CreateTopic([FromBody] TopicSetting topicSetting)
    {
        try
        {
            if (topicSetting == null)
                return BadRequest(new ProblemDetails
                {
                    Detail = "Topic setting is required",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request"
                });

            if (!ModelState.IsValid)
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                });

            topicSetting.TopicSettingId = 0;
            var topicId = await _settingsRepo.AddTopicSettingAsync(topicSetting);
            return CreatedAtAction(nameof(CreateTopic), null);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }


    [HttpPut]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> UpdateTopic([FromBody] TopicSetting topicSetting)
    {
        try
        {
            if (topicSetting == null)
                return BadRequest(new ProblemDetails
                {
                    Detail = "Topic setting is required",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request"
                });

            if (!ModelState.IsValid)
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                });

            await _settingsRepo.UpdateTopicSettingAsync(topicSetting);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }

    [HttpDelete]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> DeleteTopic([FromBody] TopicSetting topicSetting)
    {
        try
        {
            if (topicSetting == null)
                return BadRequest(new ProblemDetails
                {
                    Detail = "Topic setting is required",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request"
                });

            if (!ModelState.IsValid)
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                });

            await _settingsRepo.RemoveTopicSettingAsync(topicSetting);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }
}