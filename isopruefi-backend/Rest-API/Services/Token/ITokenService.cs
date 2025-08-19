using System.Security.Claims;

namespace Rest_API.Services.Token;

/// <summary>
/// Service responsible for generating JWT access and refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token containing the specified claims.
    /// </summary>
    /// <param name="claims">Claims to include in the token.</param>
    /// <returns>JWT access token as a string.</returns>
    string GenerateAccessToken(IEnumerable<Claim> claims);

    /// <summary>
    /// Generates a secure random refresh token.
    /// </summary>
    /// <returns>Refresh token as a base64 string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Retrieves the claims principal from an expired access token.
    /// </summary>
    /// <param name="accessToken">The expired access token.</param>
    /// <returns>Claims principal containing the token's claims.</returns>
    ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken);
}