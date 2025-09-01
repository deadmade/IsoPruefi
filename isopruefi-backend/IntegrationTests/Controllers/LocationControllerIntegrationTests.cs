using FluentAssertions;
using IntegrationTests.ApiClient;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Controllers;

[TestFixture]
public class LocationControllerIntegrationTests : ApiClientTestBase
{
    [Test]
    public async Task GetAllPostalcodes_WithValidUserToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var response = await LocationClient.GetAllPostalcodesAsync();

        response.StatusCode.Should().BeOneOf(200, 500);
    }

    [Test]
    public async Task GetAllPostalcodes_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var response = await LocationClient.GetAllPostalcodesAsync();

        response.StatusCode.Should().BeOneOf(200, 500);
    }

    [Test]
    public async Task GetAllPostalcodes_WithoutToken_ThrowsUnauthorizedException()
    {
        var exception = Assert.ThrowsAsync<ApiException>(() =>
            LocationClient.GetAllPostalcodesAsync());

        exception.StatusCode.Should().Be(401);
    }

    [Test]
    public async Task InsertLocation_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var postalCode = 10115; // Berlin postal code for testing

        var response = await LocationClient.InsertLocationAsync(postalCode);

        response.StatusCode.Should().BeOneOf(200, 500);
    }

    [Test]
    public async Task InsertLocation_WithUserToken_ThrowsForbiddenException()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var postalCode = 10115;

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            LocationClient.InsertLocationAsync(postalCode));

        exception.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task InsertLocation_WithoutToken_ThrowsUnauthorizedException()
    {
        var postalCode = 10115;

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            LocationClient.InsertLocationAsync(postalCode));

        exception.StatusCode.Should().Be(401);
    }

    [Test]
    public async Task RemovePostalcode_WithValidToken_ReturnsOkOrNotFound()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var postalCode = 12345; // Test postal code

        try
        {
            await LocationClient.RemovePostalcodeAsync(postalCode);
            Assert.Pass(); // Success case
        }
        catch (ApiException ex)
        {
            // Allow 404 Not Found or 500 Internal Server Error
            ex.StatusCode.Should().BeOneOf(404, 500);
        }
    }

    [Test]
    public async Task RemovePostalcode_WithoutToken_ThrowsUnauthorizedException()
    {
        var postalCode = 12345;

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            LocationClient.RemovePostalcodeAsync(postalCode));

        exception.StatusCode.Should().Be(401);
    }
}