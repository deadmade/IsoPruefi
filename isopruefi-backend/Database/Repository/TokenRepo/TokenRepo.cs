using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repository.TokenRepo;

/// <summary>
///     Repository implementation for managing TokenInfo entities.
/// </summary>
public class TokenRepo : ITokenRepo
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    ///     Initializes a new instance of the TokenRepo class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public TokenRepo(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    ///     Gets a token info by refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to search for.</param>
    /// <returns>The token info if found; otherwise, null.</returns>
    public async Task<TokenInfo?> GetTokenInfoByRefreshTokenAsync(string refreshToken)
    {
        return await _context.TokenInfos
            .SingleOrDefaultAsync(t => t.RefreshToken == refreshToken);
    }

    /// <summary>
    ///     Gets a token info by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The token info if found; otherwise, null.</returns>
    public async Task<TokenInfo?> GetTokenInfoByUsernameAsync(string username)
    {
        return await _context.TokenInfos
            .SingleOrDefaultAsync(t => t.Username == username);
    }

    /// <summary>
    ///     Gets the first token info for a user by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The first token info if found; otherwise, null.</returns>
    public TokenInfo? GetTokenInfoByUsernameSync(string username)
    {
        return _context.TokenInfos
            .FirstOrDefault(t => t.Username == username);
    }

    /// <summary>
    ///     Adds a new token info.
    /// </summary>
    /// <param name="tokenInfo">The token info to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddTokenInfoAsync(TokenInfo tokenInfo)
    {
        await _context.TokenInfos.AddAsync(tokenInfo);
    }

    /// <summary>
    ///     Updates an existing token info.
    /// </summary>
    /// <param name="tokenInfo">The token info to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateTokenInfoAsync(TokenInfo tokenInfo)
    {
        _context.TokenInfos.Update(tokenInfo);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Saves changes to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}