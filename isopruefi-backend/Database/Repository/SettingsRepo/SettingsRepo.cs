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
        _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
    }


    /// <inheritdoc />
    public Task<List<TopicSetting>> GetTopicSettingsAsync()
    {
        return _applicationDbContext.TopicSettings.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> AddTopicSettingAsync(TopicSetting topicSetting)
    {
        if (topicSetting == null)
            throw new ArgumentNullException(nameof(topicSetting));

        _applicationDbContext.TopicSettings.Add(topicSetting);
        return await _applicationDbContext.SaveChangesAsync();
    }
}