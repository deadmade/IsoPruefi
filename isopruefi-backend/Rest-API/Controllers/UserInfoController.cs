using Asp.Versioning;
using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest_API.Models;
using Rest_API.Services.User;

namespace Rest_API.Controllers;

/// <summary>
/// Provides comprehensive user management functionality for the IsoPruefi system.
/// Handles user data retrieval, profile updates, password changes, and account management operations.
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize]
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
    /// Retrieves a comprehensive list of all registered users in the system.
    /// </summary>
    /// <remarks>
    /// This endpoint provides administrators with complete user information for system management.
    /// 
    /// **Authorization Required**: Bearer token with Admin role
    /// 
    /// **Returned Information**:
    /// - User IDs and usernames
    /// - Account creation dates
    /// - User roles and permissions
    /// - Account status (active/inactive)
    /// - Last login information (if available)
    /// 
    /// **Use Cases**:
    /// - User administration and management
    /// - Audit and compliance reporting
    /// - System monitoring and analytics
    /// - Troubleshooting user access issues
    /// 
    /// **Example Response**:
    /// ```json
    /// [
    ///   {
    ///     "id": "123e4567-e89b-12d3-a456-426614174000",
    ///     "userName": "admin",
    ///     "email": "admin@example.com",
    ///     "roles": ["Admin", "User"],
    ///     "emailConfirmed": true,
    ///     "lockoutEnabled": false
    ///   },
    ///   {
    ///     "id": "123e4567-e89b-12d3-a456-426614174001", 
    ///     "userName": "sensor_user",
    ///     "email": "user@example.com",
    ///     "roles": ["User"],
    ///     "emailConfirmed": true,
    ///     "lockoutEnabled": false
    ///   }
    /// ]
    /// ```
    /// </remarks>
    /// <returns>A complete list of all users with their detailed information.</returns>
    /// <response code="200">Successfully retrieved all users. Returns comprehensive user list.</response>
    /// <response code="401">Authentication required. No valid JWT token provided.</response>
    /// <response code="403">Access denied. Admin role required to view all users.</response>
    /// <response code="500">Internal server error. Database connection issues or user service unavailable.</response>    [HttpGet]
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "AdminOnly")]
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
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "UserOrAdmin")]
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
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "UserOrAdmin")]
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
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "AdminOnly")]
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
    [Produces("application/json")]
    [Consumes("application/json")]
    [Authorize(Policy = "AdminOnly")]
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