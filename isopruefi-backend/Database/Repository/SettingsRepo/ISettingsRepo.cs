using Database.EntityFramework.Models;

namespace Database.Repository.SettingsRepo;

/// <summary>
///     Repository interface for accessing and managing topic settings.
/// </summary>
public interface ISettingsRepo
{
    /// <summary>
    ///     Asynchronously retrieves a list of all topic settings.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a list of
    ///     <see cref="TopicSetting" /> objects.
    /// </returns>
    Task<List<TopicSetting>> GetTopicSettingsAsync();

    /// <summary>
    ///     Asynchronously adds a new topic setting to the repository.
    /// </summary>
    /// <param name="topicSetting">The topic setting to add to the repository.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the number of entities affected by
    ///     the operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when topicSetting is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the add operation fails.</exception>
    Task<int> AddTopicSettingAsync(TopicSetting topicSetting);


    /// <summary>
    ///     Asynchronously updates a  topic setting
    /// </summary>
    /// <param name="topicSetting">The topic setting to update</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the number of entities affected by
    ///     the operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when topicSetting is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the add operation fails.</exception>
    Task<int> UpdateTopicSettingAsync(TopicSetting topicSetting);

    /// <summary>
    ///     Asynchronously removes a  topic setting
    /// </summary>
    /// <param name="topicSetting">The topic setting to remove</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the number of entities affected by
    ///     the operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when topicSetting is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the add operation fails.</exception>
    Task<int> RemoveTopicSettingAsync(TopicSetting topicSetting);
}