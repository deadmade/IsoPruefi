namespace Rest_API.Models;

/// <summary>
/// Represents a JWT token and its associated refresh token and metadata.
/// </summary>
public class JwtToken
{
    /// <summary>
    /// Gets or sets the JWT access token string.
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Gets or sets the refresh token string.
    /// </summary>
    public required string RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the expiry date and time of the JWT token.
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time of the JWT token.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user roles associated with the JWT token.
    /// </summary>
    public IList<string>? Roles { get; set; }
}