using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using Database.EntityFramework.Models;
using Database.Repository.TokenRepo;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rest_API.Models;
using Rest_API.Services.Token;

namespace Rest_API.Services.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly UserManager<ApiUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ITokenRepo _tokenRepo;

    public AuthenticationService(ILogger<AuthenticationService> logger, UserManager<ApiUser> userManager,
        RoleManager<IdentityRole> roleManager, ITokenService tokenService, ITokenRepo tokenRepo)
    {
        _tokenRepo = tokenRepo;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="input">The registration details.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a message
    /// indicating the result of the registration.
    /// </returns>
    /// <exception cref="Exception">Thrown when the registration fails.</exception>
    public async Task Register(Register input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Password))
            {
                _logger.LogError("Username or Password is empty");
                throw new AuthenticationException("Username or Password cannot be empty.");
            }


            var existingUser = await _userManager.FindByNameAsync(input.UserName);
            if (existingUser != null)
            {
                _logger.LogError("User {InputUserName} already exists", input.UserName);
                throw new Exception($"User {input.UserName} already exists.");
            }

            // Create User role if it doesn't exist
            if (await _roleManager.RoleExistsAsync(Roles.User) == false)
            {
                var roleResult = await _roleManager
                    .CreateAsync(new IdentityRole(Roles.User));

                if (roleResult.Succeeded == false)
                {
                    var roleErros = roleResult.Errors.Select(e => e.Description);
                    _logger.LogError($"Failed to create user role. Errors : {string.Join(",", roleErros)}");
                    throw new Exception($"Failed to create user role. Errors : {string.Join(",", roleErros)}");
                }
            }

            var newUser = new ApiUser { UserName = input.UserName };
            var result = await _userManager.CreateAsync(newUser, input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {NewUserUserName} has been created", newUser.UserName);
            }
            else
            {
                _logger.LogError("Error creating user {InputUserName}: {Join}", input.UserName,
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                throw new Exception($"ErrorDto: {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }

            var addUserToRoleResult = await _userManager.AddToRoleAsync(newUser, Roles.User);

            if (addUserToRoleResult.Succeeded == false)
            {
                var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                _logger.LogError("Failed to add role to the user. Errors : {Join}", string.Join(",", errors));
                throw new Exception($"Failed to add role to the user. Errors : {string.Join(",", errors)}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error creating user {InputUserName}: {EMessage}", input.UserName, e.Message);
            throw;
        }
    }

    public async Task<JwtToken> Login(Login input)
    {
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Password))
        {
            _logger.LogError("Username or Password is empty");
            throw new AuthenticationException("Username or Password cannot be empty.");
        }

        try
        {
            var user = await _userManager.FindByNameAsync(input.UserName);
            if (user == null || !await _userManager.CheckPasswordAsync(user, input.Password) ||
                string.IsNullOrEmpty(user.UserName))
            {
                _logger.LogError("Invalid Login Attempt for User {InputUserName}", input.UserName);

                if (user != null) await _userManager.AccessFailedAsync(user);

                throw new AuthenticationException("Invalid Login Attempt");
            }
            else
            {
                _logger.LogInformation("Login for User {InputUserName} successful", input.UserName);
            }

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("YourSigningKeyHere")),
                SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique identifier for the JWT
            };

            var userRoles = await _userManager.GetRolesAsync(user);

            // adding roles to the claims. So that we can get the user role from the token.
            foreach (var userRole in userRoles) claims.Add(new Claim(ClaimTypes.Role, userRole));

            var token = _tokenService.GenerateAccessToken(claims);

            var refreshToken = _tokenService.GenerateRefreshToken();

            var tokenInfo = _tokenRepo.GetTokenInfoByUsernameSync(user.UserName);

            // If tokenInfo is null for the user, create a new one
            if (tokenInfo == null)
            {
                tokenInfo = new TokenInfo
                {
                    Username = user.UserName,
                    RefreshToken = refreshToken,
                    ExpiredAt = DateTime.UtcNow.AddDays(7)
                };
                await _tokenRepo.AddTokenInfoAsync(tokenInfo);
            }
            // Else, update the refresh token and expiration
            else
            {
                tokenInfo.RefreshToken = refreshToken;
                tokenInfo.ExpiredAt = DateTime.UtcNow.AddDays(7);
                await _tokenRepo.UpdateTokenInfoAsync(tokenInfo);
            }

            await _tokenRepo.SaveChangesAsync();

            _logger.LogInformation("Jwt Token for User {InputUserName} created", input.UserName);

            return new JwtToken
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiryDate = tokenInfo.ExpiredAt,
                CreatedDate = DateTime.UtcNow
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Error logging in user {InputUserName}: {EMessage}", input.UserName, e.Message);
            throw;
        }
    }

    public async Task<JwtToken> RefreshToken(JwtToken tokenModel)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(tokenModel.Token);
            var username = principal.Identity?.Name;

            if (username == null)
            {
                _logger.LogError("Invalid token: Username is null");
                throw new AuthenticationException("Invalid token.");
            }

            var tokenInfo = await _tokenRepo.GetTokenInfoByUsernameAsync(username);
            if (tokenInfo == null || tokenInfo.RefreshToken != tokenModel.RefreshToken ||
                tokenInfo.ExpiredAt <= DateTime.UtcNow)
            {
                _logger.LogError("Invalid refresh token for user {Username}", username);
                throw new AuthenticationException("Invalid refresh token. Please login again.");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Update the existing TokenInfo with the new refresh token and expiration date
            tokenInfo.RefreshToken = newRefreshToken;
            tokenInfo.ExpiredAt = DateTime.UtcNow.AddDays(7);
            await _tokenRepo.UpdateTokenInfoAsync(tokenInfo);
            await _tokenRepo.SaveChangesAsync();

            return new JwtToken
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiryDate = tokenInfo.ExpiredAt,
                CreatedDate = DateTime.UtcNow
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Error refreshing token: {EMessage}", e.Message);
            throw;
        }
    }

    public async Task<List<ApiUser>> GetUserInformations()
    {
        try
        {
            var users = _userManager.Users;

            var userList = await users.ToListAsync();

            return userList;
        }
        catch (Exception e)
        {
            _logger.LogError("Error getting user informations: {EMessage}", e.Message);
            throw;
        }
    }

    public async Task<ApiUser?> GetUserById(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user;
        }
        catch (Exception e)
        {
            _logger.LogError("Error getting user informations: {EMessage}", e.Message);
            throw;
        }
    }

    /// <summary>
    /// Changes the password of a user.
    /// </summary>
    /// <param name="user">The Object of the user.</param>
    /// <param name="currentPassword">The current password of the user.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a message
    /// indicating the result of the password change.
    /// </returns>
    /// <exception cref="Exception">Thrown when the password change fails.</exception>
    public async Task ChangePassword(ApiUser user, string currentPassword, string newPassword)
    {
        try
        {
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password for user {UserUserName} has been changed", user.UserName);
            }
            else
            {
                _logger.LogError("Error changing password for user {UserUserName}: {Join}", user.UserName,
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                throw new Exception($"Error: {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error changing password for user {UserUserName}: {EMessage}", user.UserName, e.Message);
            throw;
        }
    }

    /// <summary>
    /// Changes the username of a user.
    /// </summary>
    /// <param name="user">The User Object of the user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a message
    /// indicating the result of the username change.
    /// </returns>
    /// <exception cref="Exception">Thrown when the username change fails.</exception>
    public async Task ChangeUser(ApiUser user)
    {
        try
        {
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Username for user {UserId} has been changed to {UserUserName}", user.Id,
                    user.UserName);
            }
            else
            {
                _logger.LogError("Error changing username for user {UserId}: {Join}", user.Id,
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                throw new Exception($"Error: {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error changing username for user {UserId}: {EMessage}", user.Id, e.Message);
            throw;
        }
    }

    public async Task<bool> DeleteUser(ApiUser user)
    {
        try
        {
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                _logger.LogInformation("User {UserId} has been deleted", user.Id);
            else
                _logger.LogError("Error deleting user {UserId}: {Join}", user.Id,
                    string.Join(" ", result.Errors.Select(e => e.Description)));

            return result.Succeeded;
        }
        catch (Exception e)
        {
            _logger.LogError("Error deleting user {UserId}: {EMessage}", user.Id, e.Message);
            throw;
        }
    }
}