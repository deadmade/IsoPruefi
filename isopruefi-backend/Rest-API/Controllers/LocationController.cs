using Asp.Versioning;
using Database.Repository.CoordinateRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest_API.Services.Temp;

namespace Rest_API.Controllers;

/// <summary>
///     Controller for managing temperature data actions. Provides endpoints for retrieving postalcodes and getting
///     locations.
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
public class LocationController : ControllerBase
{
    private readonly ICoordinateRepo _coordinateRepo;
    private readonly ILogger<LocationController> _logger;
    private readonly ITempService _tempService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LocationController" /> class.
    /// </summary>
    /// <param name="tempService">The temp service to handle temperature operations.</param>
    /// <param name="coordinateRepo">The coordinate Repo service to hande coordinates</param>
    /// <param name="logger">The logger instance for logging actions and errors.</param>
    public LocationController(ITempService tempService, ICoordinateRepo coordinateRepo,
        ILogger<LocationController> logger)
    {
        _tempService = tempService;
        _coordinateRepo = coordinateRepo;
        _logger = logger;
    }

    /// <summary>
    ///     Retrieves all saved locations.
    /// </summary>
    /// <returns>A list of all postalcodes; otherwise, NotFound.</returns>
    [HttpGet]
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> GetAllPostalcodes()
    {
        try
        {
            var postalcodes = await _tempService.ShowAvailableLocations();
            return Ok(postalcodes);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error fetching all postalcodes");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }

    /// <summary>
    ///     Checks for existence of location and if necessary inserts new location.
    /// </summary>
    /// <param name="postalcode">Defines the location.</param>
    /// <returns>Ok if successful; otherwise, an error response.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> InsertLocation(int postalcode)
    {
        try
        {
            await _tempService.GetCoordinates(postalcode);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Bad request while inserting a new location");
            return BadRequest(new ProblemDetails
            {
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request"
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while inserting a location");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }

    /// <summary>
    ///     Deletes location from the database.
    /// </summary>
    /// <param name="postalCode">Postalcode</param>
    /// <returns>Ok if successful; otherwise, an error response.</returns>
    [HttpDelete]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> RemovePostalcode(int postalCode)
    {
        try
        {
            await _coordinateRepo.DeletePostalCode(postalCode);
            return Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting postalcode");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error"
            });
        }
    }
}