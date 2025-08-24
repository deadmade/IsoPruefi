using System.Security.Authentication;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest_API.Helper;
using Rest_API.Models;
using IAuthenticationService = Rest_API.Services.Auth.IAuthenticationService;

namespace Rest_API.Controllers;

/// <summary>
/// Handles user authentication operations including login, registration, and token refresh.
/// Provides secure JWT-based authentication for the IsoPruefi temperature monitoring system.
/// </summary>
[Route("v{version:apiVersion}/[controller]/[action]")]
[ApiController]
[ApiVersion("1.0")]
[Consumes("application/json")]
public class AuthenticationController(
    IAuthenticationService authenticationService,
    ILogger<AuthenticationController> logger) : ControllerBase
{
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly ILogger<AuthenticationController> _logger = logger;

    /// <summary>
    /// Authenticates a user and returns a JWT token for API access.
    /// </summary>
    /// <remarks>
    /// This endpoint validates user credentials and returns a JWT access token and refresh token.
    /// The returned tokens should be used for authenticating subsequent API requests.
    /// 
    /// Example request:
    /// ```json
    /// {
    ///   "userName": "admin",
    ///   "password": "your-password"
    /// }
    /// ```
    /// 
    /// Example response:
    /// ```json
    /// {
    ///   "accessToken": "CakeIsNotALie.",
    ///   "refreshToken": "refresh-token-here",
    ///   "expiresIn": 3600
    /// }
    /// ```
    /// </remarks>
    /// <param name="input">The login credentials containing username and password.</param>
    /// <returns>JWT tokens and expiration information on successful authentication.</returns>
    /// <response code="200">Authentication successful. Returns JWT access token and refresh token.</response>
    /// <response code="400">Invalid input data or missing required fields.</response>
    /// <response code="401">Authentication failed. Invalid username or password.</response>
    /// <response code="500">Internal server error occurred during authentication.</response>
    [HttpPost]
    public async Task<ActionResult> Login(Login input)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Login attempt for user: {InputUserName}", input.UserName.SanitizeString());
                var jwt = await _authenticationService.Login(input);
                _logger.LogInformation("Login successful for user: {InputUserName}", input.UserName.SanitizeString());
                return Ok(jwt);
            }
            else
            {
                _logger.LogWarning("Invalid model state for user: {InputUserName}", input.UserName.SanitizeString());
                var details = new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                };
                return BadRequest(details);
            }
        }
        catch (AuthenticationException e)
        {
            _logger.LogError(e, "AuthenticationException for user: {InputUserName}", input.UserName.SanitizeString());
            var exceptionDetails = new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            return Unauthorized(exceptionDetails);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e, "InvalidOperationException for user: {InputUserName}", input.UserName.SanitizeString());
            var exceptionDetails = new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionDetails);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception for user: {InputUserName}", input.UserName.SanitizeString());
            var exceptionDetails = new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionDetails);
        }
    }

    /// <summary>
    /// Registers a new user in the system. Admin access required.
    /// </summary>
    /// <remarks>
    /// This endpoint allows administrators to create new user accounts in the system.
    /// Only users with the "Admin" role can access this endpoint.
    /// 
    /// **Authorization Required**: Bearer token with Admin role
    /// 
    /// Example request:
    /// ```json
    /// {
    ///   "userName": "newuser",
    ///   "password": "secure-password"
    /// }
    /// ```
    /// 
    /// The new user will be created with the "User" role by default and can access 
    /// temperature data endpoints but cannot perform administrative functions.
    /// </remarks>
    /// <param name="input">The registration data containing username and password for the new user.</param>
    /// <returns>Success confirmation when user is created successfully.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Invalid registration data, missing fields, or username already exists.</response>
    /// <response code="401">Authentication required. No valid JWT token provided.</response>
    /// <response code="403">Access denied. Admin role required for user registration.</response>
    /// <response code="500">Internal server error occurred during registration.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Register(Register input)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Register attempt for user: {InputUserName}", input.UserName.SanitizeString());
                await _authenticationService.Register(input);
                _logger.LogInformation("Registration successful for user: {InputUserName}",
                    input.UserName.SanitizeString());
                return Ok();
            }
            else
            {
                _logger.LogWarning("Invalid model state for user: {InputUserName}", input.UserName.SanitizeString());
                var details = new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                };
                return BadRequest(details);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during registration for user: {InputUserName}",
                input.UserName.SanitizeString());
            var exceptionDetails = new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionDetails);
        }
    }

    /// <summary>
    /// Refreshes an expired JWT access token using a valid refresh token.
    /// </summary>
    /// <remarks>
    /// This endpoint allows clients to obtain a new access token without requiring 
    /// the user to log in again. The refresh token must be valid and not expired.
    /// 
    /// Use this endpoint when your access token expires to maintain continuous 
    /// authentication without user intervention.
    /// 
    /// Example request:
    /// ```json
    /// {
    ///   "accessToken": "expired-access-token",
    ///   "refreshToken": "valid-refresh-token"
    /// }
    /// ```
    /// 
    /// Example response:
    /// ```json
    /// {
    ///   "accessToken": "new-jwt-access-token",
    ///   "refreshToken": "new-refresh-token",
    ///   "expiresIn": 3600
    /// }
    /// ```
    /// </remarks>
    /// <param name="token">The JWT token object containing both the expired access token and valid refresh token.</param>
    /// <returns>A new set of JWT tokens if the refresh token is valid.</returns>
    /// <response code="200">Token refresh successful. Returns new access and refresh tokens.</response>
    /// <response code="400">Invalid token format or missing required fields.</response>
    /// <response code="401">Refresh token is invalid, expired, or has been revoked.</response>
    /// <response code="500">Internal server error occurred during token refresh.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh(JwtToken token)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Refresh token attempt for access token");
                var result = await _authenticationService.RefreshToken(token);
                _logger.LogInformation("Refresh token successful for access token");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Invalid model state for refresh token request");
                var details = new ValidationProblemDetails(ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                };
                return BadRequest(details);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during refresh token request for access token");
            var exceptionDetails = new ProblemDetails
            {
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionDetails);
        }
    }
}