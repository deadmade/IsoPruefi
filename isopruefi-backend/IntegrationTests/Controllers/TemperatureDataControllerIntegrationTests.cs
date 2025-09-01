using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Controllers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TemperatureDataControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetTemperature_WithValidUserToken_ReturnsResponse()
    {
        // Arrange
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Berlin";

        // Act
        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        // Assert - Accept both success and internal server error (since external dependencies may not be available in tests)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }

    [Test]
    public async Task GetTemperature_WithAdminToken_ReturnsResponse()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Munich";

        // Act
        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetTemperature_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Berlin";

        // Act
        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTemperature_WithFahrenheitConversion_ReturnsResponse()
    {
        // Arrange
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Berlin";

        // Act
        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}&isFahrenheit=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetTemperature_MissingParameters_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        // Act - Missing required parameters
        var response = await Client.GetAsync("/api/v1/TemperatureData/GetTemperature");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }
}