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

    /// <summary>
    /// Inserts a new combination of postalcode and coordinates.
    /// </summary>
    Task InsertNewPostalCode(CoordinateMapping postalCodeLocation);

    /// <summary>
    /// Retrieves the coordinates of the postalcode that was inserted last.
    /// </summary>
    Task<Tuple<double, double>> GetCoordinates(int postalcode);

    /// <summary>
    /// Checks if there is an entry for that opstal code in the database.
    /// </summary>
    Task<bool> ExistsPostalCode(int postalcode);

    /// <summary>
    /// Updates the timestamp of an entry in the Coordinates Database.
    /// </summary>
    Task UpdateTime(int postalcode, DateTime newTime);
    ///
    /// </summary>
    /// <param name="topicSetting"></param>
    /// <returns></returns>
    Task<int> AddTopicSettingAsync(TopicSetting topicSetting);
}