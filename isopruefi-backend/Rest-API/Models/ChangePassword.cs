namespace Rest_API.Models;

/// <summary>
/// Represents a request to change a user's password.
/// </summary>
public class ChangePassword
{
    /// <summary>
    /// Gets or sets the unique identifier of the user whose password is to be changed.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the current password of the user.
    /// </summary>
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// Gets or sets the new password to be set for the user.
    /// </summary>
    public string? NewPassword { get; set; }
}