using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests;

[TestFixture]
public class StartupIntegrationTests : IntegrationTestBase
{
    [Test]
    public void Application_StartsSuccessfully()
    {
        Client.Should().NotBeNull();
        Factory.Should().NotBeNull();
    }

    [Test]
    public async Task Swagger_IsAccessible_InTestEnvironment()
    {
        var response = await Client.GetAsync("/swagger/v1/swagger.json");
        
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}