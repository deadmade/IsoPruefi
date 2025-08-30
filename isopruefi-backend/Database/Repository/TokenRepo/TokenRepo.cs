using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repository.TokenRepo;

/// <summary>
/// Repository implementation for managing TokenInfo entities.
/// </summary>
public class TokenRepo : ITokenRepo
{
    /// <summary>
    /// The ApplicationDbContext that is used for accessing the database.
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRepo"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public TokenRepo(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<TokenInfo?> GetTokenInfoByRefreshTokenAsync(string refreshToken)
    {
        return await _context.TokenInfos
            .SingleOrDefaultAsync(t => t.RefreshToken == refreshToken);
    }

    /// <inheritdoc />
    public async Task<TokenInfo?> GetTokenInfoByUsernameAsync(string username)
    {
        return await _context.TokenInfos
            .SingleOrDefaultAsync(t => t.Username == username);
    }

    /// <inheritdoc />
    public TokenInfo? GetTokenInfoByUsernameSync(string username)
    {
        return _context.TokenInfos
            .FirstOrDefault(t => t.Username == username);
    }

    /// <inheritdoc />
    public async Task AddTokenInfoAsync(TokenInfo tokenInfo)
    {
        await _context.TokenInfos.AddAsync(tokenInfo);
    }

    /// <inheritdoc />
    public Task UpdateTokenInfoAsync(TokenInfo tokenInfo)
    {
        _context.TokenInfos.Update(tokenInfo);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}