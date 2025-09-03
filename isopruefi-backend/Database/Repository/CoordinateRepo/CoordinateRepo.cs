using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repository.CoordinateRepo;

/// <summary>
///     Repository implementation for accessing and managing available locations.
/// </summary>
public class CoordinateRepo : ICoordinateRepo
{
    /// <summary>
    ///     The application's database context.
    /// </summary>
    private readonly ApplicationDbContext _applicationDbContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CoordinateRepo" /> class.
    /// </summary>
    /// <param name="applicationDbContext">The database context for settings.</param>
    public CoordinateRepo(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
    }

    /// <inheritdoc />
    public async Task InsertNewPostalCode(CoordinateMapping postalcodeLocation)
    {
        _applicationDbContext.CoordinateMappings.Add(postalcodeLocation);
        await _applicationDbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPostalCode(int postalcode)
    {
        var entry = await _applicationDbContext.CoordinateMappings.AnyAsync(c => c.PostalCode == postalcode);
        return entry;
    }
    
    /// <inheritdoc />
    public async Task UpdateTime(int postalCode, DateTime newTime)
    {
        var entry = await _applicationDbContext.CoordinateMappings.FirstAsync(c => c.PostalCode == postalCode);
        entry.LastUsed = newTime;

        await _applicationDbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<Tuple<int, string>>> GetAllLocations()
    {
        var result = await _applicationDbContext.CoordinateMappings
            .Select(c => new Tuple<int, string>(c.PostalCode, c.Location))
            .ToListAsync();

        return result;
    }
    
    /// <inheritdoc />
    public async Task<CoordinateMapping?> GetLocation()
    {
        var result = await _applicationDbContext.CoordinateMappings
            .OrderByDescending(c => c.LastUsed)
            .FirstOrDefaultAsync();

        return result;
    }
    
    /// <inheritdoc />
    public async Task<CoordinateMapping?> GetLocation(string place)
    {
        var result = await _applicationDbContext.CoordinateMappings
            .FirstOrDefaultAsync(c => c.Location == place);

        return result;
    }

    /// <inheritdoc />
    public async Task<CoordinateMapping?> GetUnlockedLocation()
    {
        await using var transaction = _applicationDbContext.Database.BeginTransaction();

        var result = await _applicationDbContext.CoordinateMappings
            .FromSqlRaw(
                @"SELECT * FROM ""CoordinateMappings"" WHERE ""LockedUntil"" IS NULL OR ""LockedUntil"" < NOW() ORDER BY ""LastUsed"" ASC NULLS FIRST LIMIT 1 FOR UPDATE SKIP LOCKED")
            .FirstOrDefaultAsync();
        if (result != null)
        {
            result.LastUsed = DateTime.UtcNow;
            result.LockedUntil = DateTime.UtcNow.AddMinutes(1);
            await _applicationDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }

        await transaction.CommitAsync();
        return null;
    }
    
    /// <inheritdoc />
    public async Task DeletePostalCode(int postalcode)
    {
        await _applicationDbContext.CoordinateMappings.Where(c => c.PostalCode == postalcode).ExecuteDeleteAsync();
    }
}