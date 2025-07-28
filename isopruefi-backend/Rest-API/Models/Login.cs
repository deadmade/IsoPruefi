using System.ComponentModel.DataAnnotations;

namespace Rest_API.Models;

/// <summary>
/// Represents the login credentials for a user.
/// </summary>
public class Login
{
    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    [Required]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the password of the user.
    /// </summary>
    [Required]
    public string Password { get; set; }
}