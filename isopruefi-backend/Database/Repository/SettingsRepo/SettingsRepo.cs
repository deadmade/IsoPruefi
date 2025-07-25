using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repository.SettingsRepo;

/// <summary>
/// Repository implementation for accessing and managing topic settings in the database.
/// </summary>
public class SettingsRepo : ISettingsRepo
{
    private SettingsContext _settingsContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepo"/> class with the specified settings context.
    /// </summary>
    /// <param name="settingsContext">The database context for settings.</param>
    public SettingsRepo(SettingsContext settingsContext)
    {
        _settingsContext = settingsContext;
    }


    /// <inheritdoc />
    public Task<List<TopicSetting>> GetTopicSettingsAsync()
    {
        return _settingsContext.TopicSettings.ToListAsync();
    }
}