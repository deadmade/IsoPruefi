using IntegrationTests.ApiClient;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that require generated API client instances for testing API endpoints.
/// Extends IntegrationTestBase with pre-configured API clients for all available controllers.
/// </summary>
[TestFixture]
public abstract class ApiClientTestBase : IntegrationTestBase
{
    /// <summary>
    /// One-time setup to initialize API clients using the test HTTP client and base URL.
    /// </summary>
    [OneTimeSetUp]
    public override async Task OneTimeSetUp()
    {
        await base.OneTimeSetUp();

        var baseUrl = Client.BaseAddress?.ToString() ?? "http://localhost";

        AuthenticationClient = new AuthenticationClient(baseUrl, Client);
        LocationClient = new LocationClient(baseUrl, Client);
        TemperatureDataClient = new TemperatureDataClient(baseUrl, Client);
        TopicClient = new TopicClient(baseUrl, Client);
        UserInfoClient = new UserInfoClient(baseUrl, Client);
    }

    /// <summary>
    /// Gets the Authentication API client for login, registration, and JWT token operations.
    /// </summary>
    protected AuthenticationClient AuthenticationClient { get; private set; } = null!;
    
    /// <summary>
    /// Gets the Location API client for postal code and geographic location management.
    /// </summary>
    protected LocationClient LocationClient { get; private set; } = null!;
    
    /// <summary>
    /// Gets the Temperature Data API client for weather and sensor data retrieval.
    /// </summary>
    protected TemperatureDataClient TemperatureDataClient { get; private set; } = null!;
    
    /// <summary>
    /// Gets the Topic API client for MQTT topic and sensor configuration management.
    /// </summary>
    protected TopicClient TopicClient { get; private set; } = null!;
    
    /// <summary>
    /// Gets the User Info API client for user profile and account management operations.
    /// </summary>
    protected UserInfoClient UserInfoClient { get; private set; } = null!;
}