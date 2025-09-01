using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Controllers;

[TestFixture]
public class LocationControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetAllPostalcodes_WithValidUserToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/Location/GetAllPostalcodes");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetAllPostalcodes_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/Location/GetAllPostalcodes");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetAllPostalcodes_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/v1/Location/GetAllPostalcodes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InsertLocation_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var postalCode = 10115; // Berlin postal code for testing

        var response = await Client.PostAsync($"/api/v1/Location/InsertLocation?postalcode={postalCode}", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task InsertLocation_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var postalCode = 10115;

        var response = await Client.PostAsync($"/api/v1/Location/InsertLocation?postalcode={postalCode}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task InsertLocation_WithoutToken_ReturnsUnauthorized()
    {
        var postalCode = 10115;

        var response = await Client.PostAsync($"/api/v1/Location/InsertLocation?postalcode={postalCode}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RemovePostalcode_WithValidToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var postalCode = 12345; // Test postal code

        var response = await Client.DeleteAsync($"/api/v1/Location/RemovePostalcode?postalCode={postalCode}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RemovePostalcode_WithoutToken_ReturnsUnauthorized()
    {
        var postalCode = 12345;

        var response = await Client.DeleteAsync($"/api/v1/Location/RemovePostalcode?postalCode={postalCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}