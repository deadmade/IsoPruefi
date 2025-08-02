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
    /// Asynchronously adds a new topic setting to the repository.
    /// </summary>
    /// <param name="topicSetting">The topic setting to add to the repository.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of entities affected by the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when topicSetting is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the add operation fails.</exception>
    Task<int> AddTopicSettingAsync(TopicSetting topicSetting);
    
    /// <summary>
    /// Inserts a new combination of postalcode and coordinates.
    /// </summary>
    /// <param name="postalCodeLocation">A CoordinateMapping instance that will be saved in the database.</param>
    Task InsertNewPostalCode(CoordinateMapping postalCodeLocation);

    /// <summary>
    /// Retrieves the coordinates of the postalcode that was inserted last.
    /// </summary>
    /// <returns>Returns coordinates of the location that was last chosen by the User.</returns>
    Task<Tuple<string, double, double>> GetLocation();

    /// <summary>
    /// Checks if there is an entry for that opstal code in the database.
    /// </summary>
    /// <param name="postalcode">Defines which entry will be checked.</param>
    /// <returns>Returns a boolean values for the existence of an entry in the database associated with the postalcode.</returns>
    Task<bool> ExistsPostalCode(int postalcode);

    /// <summary>
    /// Updates the timestamp of an entry in the Coordinates Database.
    /// </summary>
    /// <param name="postalcode">Defines which entry will be updated.</param>
    /// <param name="newTime">Defines the new time for that entry.</param>
    Task UpdateTime(int postalcode, DateTime newTime);
}