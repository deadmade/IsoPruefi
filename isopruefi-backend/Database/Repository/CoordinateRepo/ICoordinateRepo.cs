using Database.EntityFramework.Models;

namespace Database.Repository.CoordinateRepo;

/// <summary>
///     Repository interface for accessing and managing locations.
/// </summary>
public interface ICoordinateRepo
{
    /// <summary>
    ///     Inserts a new combination of postalcode and coordinates.
    /// </summary>
    /// <param name="postalCodeLocation">A CoordinateMapping instance that will be saved in the database.</param>
    Task InsertNewPostalCode(CoordinateMapping postalCodeLocation);

    /// <summary>
    ///     Deletes postalcode from the database.
    /// </summary>
    /// <param name="postalCode">postalcode</param>
    Task DeletePostalCode(int postalCode);

    /// <summary>
    ///     Checks if there is an entry for that postal code in the database.
    /// </summary>
    /// <param name="postalcode">postalcode</param>
    /// <returns>Returns a boolean value.</returns>
    Task<bool> ExistsPostalCode(int postalcode);
    
    /// <summary>
    ///     Retrieves the coordinates of the postalcode that was inserted last.
    /// </summary>
    /// <param name="place">Name of the location</param>
    /// <returns>Returns coordinates of the location that was last chosen by the User.</returns>
    Task<CoordinateMapping?> GetLocation(string place);

    /// <summary>
    ///     Gets all postalcodes that are saved in the database.
    /// </summary>
    /// <returns>List with all postalcodes.</returns>
    Task<List<Tuple<int, string>>> GetAllLocations();

    /// <summary>
    ///     Gets the next unlocked entry in CoordinateMappings and locks it for the next minute.
    /// </summary>
    Task<CoordinateMapping?> GetUnlockedLocation();
}