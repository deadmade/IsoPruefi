using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Rest_API.Services.Auth;
using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Authorization;
using Rest_API.Models;
using Rest_API.Services.User;

namespace Rest_API.Controllers;

/// <summary>
/// Controller for managing user information and user-related actions.
/// Provides endpoints for retrieving, updating, and deleting user data.
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
public class UserInfoController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserInfoController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInfoController"/> class.
    /// </summary>
    /// <param name="userService">The user service to handle user operations.</param>
    /// <param name="logger">The logger instance for logging actions and errors.</param>
    public UserInfoController(IUserService userService, ILogger<UserInfoController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all users from the system.
    /// </summary>
    /// <returns>A list of all users.</returns>
    [HttpGet]
   [Authorize] 
    public async Task<ActionResult> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Fetching all users");
            var users = await _userService.GetUserInformations();
            return Ok(users);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error fetching all users");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = e.Message });
        }
    }

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user information if found; otherwise, NotFound.</returns>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetUserById(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching user by ID: {UserId}", userId);
            var user = await _userService.GetUserById(userId);
            if (user == null)
                return NotFound();
            return Ok(user);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error fetching user by ID: {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = e.Message });
        }
    }

    /// <summary>
    /// Changes the password for a user.
    /// </summary>
    /// <param name="input">The change password request containing user ID, current password, and new password.</param>
    /// <returns>Ok if successful; otherwise, an error response.</returns>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePassword input)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for ChangePassword");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        try
        {
            var user = await _userService.GetUserById(input.UserId);
            if (user == null)
                return NotFound();
            await _userService.ChangePassword(user, input.CurrentPassword, input.NewPassword);
            return Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error changing password for user: {UserId}", input.UserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = e.Message });
        }
    }

    /// <summary>
    /// Updates user information.
    /// </summary>
    /// <param name="user">The user object with updated information.</param>
    /// <returns>Ok if successful; otherwise, an error response.</returns>
    [HttpPut]
    [Authorize]
    public async Task<ActionResult> ChangeUser([FromBody] ApiUser user)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for ChangeUser");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        try
        {
            await _userService.ChangeUser(user);
            return Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error changing user info for user: {UserId}", user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = e.Message });
        }
    }

    /// <summary>
    /// Deletes a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns>Ok if successful; otherwise, an error response.</returns>
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult> DeleteUser(string userId)
    {
        try
        {
            var user = await _userService.GetUserById(userId);
            if (user == null)
                return NotFound();
            var result = await _userService.DeleteUser(user);
            if (result)
                return Ok();
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Detail = "Failed to delete user." });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting user: {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = e.Message });
        }
    }
}