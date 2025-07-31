using Database.EntityFramework.Models;

namespace Rest_API.Services.User;

/// <summary>
/// Service interface for user management operations including user retrieval, updates, and deletion.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a list of all users in the system.
    /// </summary>
    /// <returns>A task containing a list of all API users.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks sufficient permissions.</exception>
    Task<List<ApiUser>> GetUserInformations();

    /// <summary>
    /// Retrieves a specific user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <returns>A task containing the user if found, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<ApiUser?> GetUserById(string userId);

    /// <summary>
    /// Changes a user's password after validating the current password.
    /// </summary>
    /// <param name="user">The user whose password will be changed.</param>
    /// <param name="currentPassword">The user's current password for validation.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <returns>A task representing the asynchronous password change operation.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when current password is incorrect.</exception>
    Task ChangePassword(ApiUser user, string currentPassword, string newPassword);

    /// <summary>
    /// Updates user information in the system.
    /// </summary>
    /// <param name="user">The user object with updated information.</param>
    /// <returns>A task representing the asynchronous user update operation.</returns>
    /// <exception cref="ArgumentException">Thrown when user is null or contains invalid data.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user update fails.</exception>
    Task ChangeUser(ApiUser user);

    /// <summary>
    /// Deletes a user from the system.
    /// </summary>
    /// <param name="user">The user to delete from the system.</param>
    /// <returns>A task containing true if deletion was successful, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when user is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user deletion fails.</exception>
    Task<bool> DeleteUser(ApiUser user);
}