using Database.EntityFramework.Models;

namespace Database.Repository.CoordinateRepo;

/// <summary>
/// Repository interface for sccessing and managing locations.
/// </summary>
public interface ICoordinateRepo
{
    /// <summary>
    /// Inserts a new combination of postalcode and coordinates.
    /// </summary>
    /// <param name="postalCodeLocation">A CoordinateMapping instance that will be saved in the database.</param>
    Task InsertNewPostalCode(CoordinateMapping postalCodeLocation);

    /// <summary>
    /// Retrieves the coordinates of the postalcode that was inserted last.
    /// </summary>
    /// <returns>Returns coordinates of the location that was last chosen by the User.</returns>
    Task<CoordinateMapping?> GetLocation();

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

    /// <summary>
    /// Gets all postalcodes that are saved in the database.
    /// </summary>
    /// <returns>List with all postalcodes.</returns>
    Task<List<Tuple<int, string>>> GetAllLocations();
    
    /// <summary>
    /// Gets the next unlocked entry in CoordinateMappings and locks it for the next minute.
    /// </summary>
    /// <returns></returns>
    Task<CoordinateMapping?> GetUnlockedLocation();
}