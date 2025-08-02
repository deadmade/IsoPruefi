using System.Security.Authentication;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rest_API.Models;
using IAuthenticationService = Rest_API.Services.Auth.IAuthenticationService;

namespace Rest_API.Controllers;

/// <summary>
/// Controller zur Handhabung von Authentifizierungsaktionen.
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
    /// Die Login-Methode.
    /// </summary>
    /// <param name="input">Login-Datenübertragungsobjekt.</param>
    /// <returns>JWT-Token oder Fehlerdetails.</returns>
    /// <response code="200">JWT-Token bei erfolgreichem Login.</response>
    /// <response code="400">Ungültige Modelldaten.</response>
    /// <response code="401">Authentifizierungsfehler.</response>
    /// <response code="500">Interner Serverfehler.</response>
    [HttpPost]
    public async Task<ActionResult> Login(Login input)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Login attempt for user: {InputUserName}", input.UserName);
                var jwt = await _authenticationService.Login(input);
                _logger.LogInformation("Login successful for user: {InputUserName}", input.UserName);
                return Ok(jwt);
            }
            else
            {
                _logger.LogWarning("Invalid model state for user: {InputUserName}", input.UserName);
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
            _logger.LogError(e, "AuthenticationException for user: {InputUserName}", input.UserName);
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
            _logger.LogError(e, "InvalidOperationException for user: {InputUserName}", input.UserName);
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
            _logger.LogError(e, "Exception for user: {InputUserName}", input.UserName);
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
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="input">The registration data containing user credentials and information.</param>
    /// <returns>An IActionResult indicating the result of the registration operation.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Invalid registration data or user already exists.</response>
    /// <response code="500">Internal server error occurred during registration.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult> Register(Register input)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Register attempt for user: {InputUserName}", input.UserName);
                await _authenticationService.Register(input);
                _logger.LogInformation("Registration successful for user: {InputUserName}", input.UserName);
                return Ok();
            }
            else
            {
                _logger.LogWarning("Invalid model state for user: {InputUserName}", input.UserName);
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
            _logger.LogError(e, "Exception during registration for user: {InputUserName}", input.UserName);
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
    /// Handles the refresh token request. Validates the incoming JWT token and issues a new access token if valid.
    /// </summary>
    /// <param name="token">The JWT token containing the refresh token and access token.</param>
    /// <returns>Returns a new access token if the refresh is successful; otherwise, returns an error response.</returns>
    [HttpPost]
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