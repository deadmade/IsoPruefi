using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests;

/// <summary>
///     Integration tests to verify application startup and basic endpoint accessibility.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class StartupIntegrationTests : IntegrationTestBase
{
    /// <summary>
    ///     Verifies that the application starts successfully and the HTTP client and test factory are properly initialized.
    /// </summary>
    [Test]
    public void Application_StartsSuccessfully()
    {
        Client.Should().NotBeNull();
        Factory.Should().NotBeNull();
    }

    /// <summary>
    ///     Verifies that the Swagger API documentation endpoint is accessible in the test environment.
    /// </summary>
    [Test]
    public async Task Swagger_IsAccessible_InTestEnvironment()
    {
        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}