namespace Rest_API.Services.Temp;

public interface ITempService
{
    /// <summary>
    /// Retrieves the coordinates for the postalcode chosen by the admin if there is no entry in the database already.
    /// </summary>
    /// <param name="postalCode">The postalcode of the city that was chosen by the admin.</param>
    Task GetCoordinates(int postalCode);

    /// <summary>
    /// Retrieves all postalcodes that are saved in the database for the user to chose from.
    /// </summary>
    /// <returns>A list containing all postalcodes.</returns>
    Task<List<int>?> ShowAvailablePostalcodes();
}