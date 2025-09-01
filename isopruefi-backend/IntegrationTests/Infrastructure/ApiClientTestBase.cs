using IntegrationTests.ApiClient;

namespace IntegrationTests.Infrastructure;

[TestFixture]
public abstract class ApiClientTestBase : IntegrationTestBase
{
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

    protected AuthenticationClient AuthenticationClient { get; private set; } = null!;
    protected LocationClient LocationClient { get; private set; } = null!;
    protected TemperatureDataClient TemperatureDataClient { get; private set; } = null!;
    protected TopicClient TopicClient { get; private set; } = null!;
    protected UserInfoClient UserInfoClient { get; private set; } = null!;
}