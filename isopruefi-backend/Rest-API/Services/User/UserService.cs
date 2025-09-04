using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Rest_API.Services.User;

/// <summary>
///     Provides user-related operations such as retrieving, updating, deleting users, and changing passwords.
/// </summary>
public class UserService : IUserService
{
    /// <summary>
    ///     Logger instance used to capture diagnostic and error information for the <see cref="UserService"/>.
    /// </summary>
    private readonly ILogger<UserService> _logger;
    
    /// <summary>
    ///     ASP.NET Core Identity UserManager used to manage <see cref="ApiUser"/> accounts.
    /// </summary>
    private readonly UserManager<ApiUser> _userManager;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserService" /> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging actions and errors.</param>
    /// <param name="userManager">The user manager for user operations.</param>
    public UserService(ILogger<UserService> logger, UserManager<ApiUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
    
    /// <inheritdoc />
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
                var errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                _logger.LogError("Error changing password for user {UserUserName}: {ErrorMessage}", user.UserName,
                    errorMessage);
                throw new Exception($"Error: {errorMessage}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error changing password for user {UserUserName}: {EMessage}", user.UserName, e.Message);
            throw;
        }
    }

    /// <inheritdoc />
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
                var errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                _logger.LogError("Error changing username for user {UserId}: {ErrorMessage}", user.Id, errorMessage);
                throw new Exception($"Error: {errorMessage}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error changing username for user {UserId}: {EMessage}", user.Id, e.Message);
            throw;
        }
    }

    /// <inheritdoc />
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