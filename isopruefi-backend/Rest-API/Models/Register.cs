using System.ComponentModel.DataAnnotations;

namespace Rest_API.Models;

/// <summary>
/// Represents the registration credentials for a new user.
/// </summary>
public class Register
{
    /// <summary>
    /// Gets or sets the username for the new user.
    /// </summary>
    [Required]
    public required string UserName { get; set; }

    /// <summary>
    /// Gets or sets the password for the new user.
    /// </summary>
    [Required]
    public required string Password { get; set; }
}