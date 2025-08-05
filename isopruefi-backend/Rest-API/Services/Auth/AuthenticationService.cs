using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using Database.EntityFramework.Models;
using Database.Repository.TokenRepo;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rest_API.Helper;
using Rest_API.Models;
using Rest_API.Services.Token;

namespace Rest_API.Services.Auth;

/// <inheritdoc />
public class AuthenticationService(
    ILogger<AuthenticationService> logger,
    UserManager<ApiUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ITokenService tokenService,
    ITokenRepo tokenRepo)
    : IAuthenticationService
{
    /// <inheritdoc />
    public async Task Register(Register input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Password))
            {
                logger.LogError("Username or Password is empty");
                throw new AuthenticationException("Username or Password cannot be empty.");
            }


            var existingUser = await userManager.FindByNameAsync(input.UserName);
            if (existingUser != null)
            {
                logger.LogError("User {InputUserName} already exists", input.UserName.SanitizeString());
                throw new AuthenticationException($"User {input.UserName} already exists.");
            }

            var newUser = new ApiUser { UserName = input.UserName };
            var result = await userManager.CreateAsync(newUser, input.Password);
            if (result.Succeeded)
            {
                logger.LogInformation("User {NewUserUserName} has been created", newUser.UserName);
            }
            else
            {
                var errorDescriptions = string.Join(" ", result.Errors.Select(e => e.Description));
                logger.LogError("Error creating user {InputUserName}: {Join}", input.UserName.SanitizeString(),
                    errorDescriptions);
                throw new Exception($"ErrorDto: {errorDescriptions}");
            }

            var addUserToRoleResult = await userManager.AddToRoleAsync(newUser, Roles.User);

            if (addUserToRoleResult.Succeeded == false)
            {
                var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                logger.LogError("Failed to add role to the user. Errors : {Join}", string.Join(",", errors));
                throw new Exception($"Failed to add role to the user. Errors : {string.Join(",", errors)}");
            }
        }
        catch (Exception e)
        {
            logger.LogError("Error creating user {InputUserName}: {EMessage}", input.UserName.SanitizeString(),
                e.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<JwtToken> Login(Login input)
    {
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Password))
        {
            logger.LogError("Username or Password is empty");
            throw new AuthenticationException("Username or Password cannot be empty.");
        }

        try
        {
            var user = await userManager.FindByNameAsync(input.UserName);
            if (user == null || !await userManager.CheckPasswordAsync(user, input.Password) ||
                string.IsNullOrEmpty(user.UserName))
            {
                logger.LogError("Invalid Login Attempt for User {InputUserName}", input.UserName.SanitizeString());

                if (user != null) await userManager.AccessFailedAsync(user);

                throw new AuthenticationException("Invalid Login Attempt");
            }
            else
            {
                logger.LogInformation("Login for User {InputUserName} successful", input.UserName.SanitizeString());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique identifier for the JWT
            };

            var userRoles = await userManager.GetRolesAsync(user);

            // adding roles to the claims. So that we can get the user role from the token.
            foreach (var userRole in userRoles) claims.Add(new Claim(ClaimTypes.Role, userRole));

            var token = tokenService.GenerateAccessToken(claims);

            var refreshToken = tokenService.GenerateRefreshToken();

            var tokenInfo = tokenRepo.GetTokenInfoByUsernameSync(user.UserName);

            // If tokenInfo is null for the user, create a new one
            if (tokenInfo == null)
            {
                tokenInfo = new TokenInfo
                {
                    Username = user.UserName,
                    RefreshToken = refreshToken,
                    ExpiredAt = DateTime.UtcNow.AddDays(7)
                };
                await tokenRepo.AddTokenInfoAsync(tokenInfo);
            }
            // Else, update the refresh token and expiration
            else
            {
                tokenInfo.RefreshToken = refreshToken;
                tokenInfo.ExpiredAt = DateTime.UtcNow.AddDays(7);
                await tokenRepo.UpdateTokenInfoAsync(tokenInfo);
            }

            await tokenRepo.SaveChangesAsync();

            logger.LogInformation("Jwt Token for User {InputUserName} created", input.UserName.SanitizeString());

            return new JwtToken
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiryDate = tokenInfo.ExpiredAt,
                CreatedDate = DateTime.UtcNow,
                Roles = userRoles
            };
        }
        catch (Exception e)
        {
            logger.LogError("Error logging in user {InputUserName}: {EMessage}", input.UserName.SanitizeString(),
                e.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<JwtToken> RefreshToken(JwtToken tokenModel)
    {
        try
        {
            var principal = tokenService.GetPrincipalFromExpiredToken(tokenModel.Token);
            var username = principal.Identity?.Name;

            if (username == null)
            {
                logger.LogError("Invalid token: Username is null");
                throw new AuthenticationException("Invalid token.");
            }

            var tokenInfo = await tokenRepo.GetTokenInfoByUsernameAsync(username);
            if (tokenInfo == null || tokenInfo.RefreshToken != tokenModel.RefreshToken ||
                tokenInfo.ExpiredAt <= DateTime.UtcNow)
            {
                logger.LogError("Invalid refresh token for user {Username}", username);
                throw new AuthenticationException("Invalid refresh token. Please login again.");
            }

            var newAccessToken = tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = tokenService.GenerateRefreshToken();

            // Update the existing TokenInfo with the new refresh token and expiration date
            tokenInfo.RefreshToken = newRefreshToken;
            tokenInfo.ExpiredAt = DateTime.UtcNow.AddDays(7);
            await tokenRepo.UpdateTokenInfoAsync(tokenInfo);
            await tokenRepo.SaveChangesAsync();

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
            logger.LogError("Error refreshing token: {EMessage}", e.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ApiUser>> GetUserInformations()
    {
        try
        {
            var users = userManager.Users;

            var userList = await users.ToListAsync();

            return userList;
        }
        catch (Exception e)
        {
            logger.LogError("Error getting user informations: {EMessage}", e.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApiUser?> GetUserById(string userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            return user;
        }
        catch (Exception e)
        {
            logger.LogError("Error getting user informations: {EMessage}", e.Message);
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
            var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                logger.LogInformation("Password for user {UserUserName} has been changed", user.UserName);
            }
            else
            {
                logger.LogError("Error changing password for user {UserUserName}: {Join}", user.UserName,
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                throw new Exception($"Error: {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }
        }
        catch (Exception e)
        {
            logger.LogError("Error changing password for user {UserUserName}: {EMessage}", user.UserName, e.Message);
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
            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                logger.LogInformation("Username for user {UserId} has been changed to {UserUserName}", user.Id,
                    user.UserName);
            }
            else
            {
                logger.LogError("Error changing username for user {UserId}: {Join}", user.Id,
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                throw new Exception($"Error: {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }
        }
        catch (Exception e)
        {
            logger.LogError("Error changing username for user {UserId}: {EMessage}", user.Id, e.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUser(ApiUser user)
    {
        try
        {
            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
                logger.LogInformation("User {UserId} has been deleted", user.Id);
            else
                logger.LogError("Error deleting user {UserId}: {Join}", user.Id,
                    string.Join(" ", result.Errors.Select(e => e.Description)));

            return result.Succeeded;
        }
        catch (Exception e)
        {
            logger.LogError("Error deleting user {UserId}: {EMessage}", user.Id, e.Message);
            throw;
        }
    }
}