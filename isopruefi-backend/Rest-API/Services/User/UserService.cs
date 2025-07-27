using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Rest_API.Services.User;

/// <summary>
/// Provides user-related operations such as retrieving, updating, deleting users, and changing passwords.
/// </summary>
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly UserManager<ApiUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging actions and errors.</param>
    /// <param name="userManager">The user manager for user operations.</param>
    public UserService(ILogger<UserService> logger, UserManager<ApiUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Retrieves all users from the system.
    /// </summary>
    /// <returns>A list of all users.</returns>
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

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user information if found; otherwise, null.</returns>
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
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Deletes a user from the system.
    /// </summary>
    /// <param name="user">The user to delete.</param>
    /// <returns>True if the user was deleted successfully; otherwise, false.</returns>
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