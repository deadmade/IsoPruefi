using System.ComponentModel.DataAnnotations;

namespace Database.EntityFramework.Models;

/// <summary>
///     Represents a refresh token entry for a user, including expiration and token details.
/// </summary>
public class TokenInfo
{
    /// <summary>
    ///     Gets or sets the unique identifier for the token entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the username associated with the refresh token.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the refresh token string.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the expiration date and time of the refresh token.
    /// </summary>
    [Required]
    public DateTime ExpiredAt { get; set; }
}