using Database.EntityFramework.Models;

namespace Database.Repository.SettingsRepo;

/// <summary>
/// Repository interface for accessing and managing topic settings.
/// </summary>
public interface ISettingsRepo
{
    /// <summary>
    /// Asynchronously retrieves a list of all topic settings.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TopicSetting"/> objects.</returns>
    Task<List<TopicSetting>> GetTopicSettingsAsync();
}