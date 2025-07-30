using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repository.SettingsRepo;

/// <summary>
/// Repository implementation for accessing and managing topic settings in the database.
/// </summary>
public class SettingsRepo : ISettingsRepo
{
    private ApplicationDbContext _applicationDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepo"/> class with the specified settings context.
    /// </summary>
    /// <param name="applicationDbContext">The database context for settings.</param>
    public SettingsRepo(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }


    /// <inheritdoc />
    public Task<List<TopicSetting>> GetTopicSettingsAsync()
    {
        return _applicationDbContext.TopicSettings.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> AddTopicSettingAsync(TopicSetting topicSetting)
    {
        _applicationDbContext.TopicSettings.Add(topicSetting);
        return await _applicationDbContext.SaveChangesAsync();
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
        var entry = await _applicationDbContext.CoordinateMappings.FirstOrDefaultAsync(c => c.PostalCode == postalcode);
        if (entry != null)
        {
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public async Task UpdateTime(int postalCode, DateTime newTime)
    {
        var entry = await _applicationDbContext.CoordinateMappings.FirstAsync(c => c.PostalCode == postalCode);
        entry.LastUsed = newTime;

        await _applicationDbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Tuple<double, double>> GetCoordinates()
    {
        var result = await _applicationDbContext.CoordinateMappings
            .OrderByDescending(c => c.LastUsed)
            .FirstOrDefaultAsync();
        var coordinates = new Tuple<double, double>(result.Latitude, result.Longitude);
        return coordinates;
    }
}