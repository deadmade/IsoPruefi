using Database.EntityFramework.Models;

namespace Database.Repository.TokenRepo;

/// <summary>
/// Repository interface for managing TokenInfo entities.
/// </summary>
public interface ITokenRepo
{
    /// <summary>
    /// Gets a token info by refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to search for.</param>
    /// <returns>The token info if found; otherwise, null.</returns>
    Task<TokenInfo?> GetTokenInfoByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Gets a token info by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The token info if found; otherwise, null.</returns>
    Task<TokenInfo?> GetTokenInfoByUsernameAsync(string username);

    /// <summary>
    /// Gets the first token info for a user by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The first token info if found; otherwise, null.</returns>
    TokenInfo? GetTokenInfoByUsernameSync(string username);

    /// <summary>
    /// Adds a new token info.
    /// </summary>
    /// <param name="tokenInfo">The token info to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddTokenInfoAsync(TokenInfo tokenInfo);

    /// <summary>
    /// Updates an existing token info.
    /// </summary>
    /// <param name="tokenInfo">The token info to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateTokenInfoAsync(TokenInfo tokenInfo);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveChangesAsync();
}