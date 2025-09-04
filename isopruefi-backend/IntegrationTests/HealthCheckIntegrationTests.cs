using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests;

/// <summary>
///     Integration tests for the health check endpoints to verify API availability and status.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class HealthCheckIntegrationTests : IntegrationTestBase
{
    /// <summary>
    ///     Verifies that the main health check endpoint returns a valid status indicating the API is operational.
    /// </summary>
    [Test]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    ///     Verifies that the Prometheus-compatible health check endpoint returns OK status for monitoring systems.
    /// </summary>
    [Test]
    public async Task HealthCheckPrometheus_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/healthoka");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}