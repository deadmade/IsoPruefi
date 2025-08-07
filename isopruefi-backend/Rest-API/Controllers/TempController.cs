using Asp.Versioning;
using Database.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest_API.Services.Temp;

namespace Rest_API.Controllers;

/// <summary>
/// Controller for managing temperature data actions. Provides endpoints for retrieving postalcodes and getting locations.
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
public class TempController : ControllerBase
{
    private readonly ITempService _tempService;
    private readonly ILogger<TempController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempController"/> class.
    /// </summary>
    /// <param name="tempService">The temp service to handle temperature operations.</param>
    /// <param name="logger">The logger instance for logging actions and errors.</param>
    public TempController(ITempService tempService, ILogger<TempController> logger)
    {
        _tempService = tempService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all saved locations.
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
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = e.Message });
        }
    }

    /// <summary>
    /// Checks for existence of location and if necessary inserts new location.
    /// </summary>
    /// <param name="postalcode">Defines the location.</param>
    /// <returns>Ok if successful; otherwise, an error response.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> InsertLocation([FromBody] int postalcode)
    {
        try
        {
            await _tempService.GetCoordinates(postalcode);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Forbidden while inserting a new location");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while inserting a location");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = e.Message });
        }
    }
}