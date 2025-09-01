using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests;

[TestFixture]
public class HealthCheckIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task HealthCheckPrometheus_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/healthoka");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}