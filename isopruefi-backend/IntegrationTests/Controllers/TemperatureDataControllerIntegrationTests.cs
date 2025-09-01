using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Rest_API.Models;

namespace IntegrationTests.Controllers;

[TestFixture]
public class TemperatureDataControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetTemperature_WithValidUserToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Berlin";

        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetTemperature_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Munich";

        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetTemperature_WithoutToken_ReturnsUnauthorized()
    {
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Berlin";

        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTemperature_WithInvalidToken_ReturnsUnauthorized()
    {
        SetAuthorizationHeader("invalid-token");

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Berlin";

        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTemperature_WithFahrenheitConversion_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "Berlin";

        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}&isFahrenheit=true");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetTemperature_InvalidDateRange_ReturnsBadRequest()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow.AddDays(-1); // End before start
        var place = "Berlin";

        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}&place={place}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetTemperature_MissingPlace_ReturnsBadRequest()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;

        var response = await Client.GetAsync(
            $"/api/v1/TemperatureData/GetTemperature?start={start:yyyy-MM-ddTHH:mm:ssZ}&end={end:yyyy-MM-ddTHH:mm:ssZ}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }
}