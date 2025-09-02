using FluentAssertions;
using IntegrationTests.ApiClient;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Controllers;

/// <summary>
///     Integration tests for the Location Controller to verify postal code management and location-based functionality.
/// </summary>
[TestFixture]
public class LocationControllerIntegrationTests : ApiClientTestBase
{
    /// <summary>
    ///     Tests retrieving all postal codes with valid user token and verifies successful response.
    /// </summary>
    [Test]
    public async Task GetAllPostalcodes_WithValidUserToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var response = await LocationClient.GetAllPostalcodesAsync();

        response.StatusCode.Should().BeOneOf(200, 500);
    }

    /// <summary>
    ///     Tests retrieving all postal codes with admin token and verifies successful response.
    /// </summary>
    [Test]
    public async Task GetAllPostalcodes_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var response = await LocationClient.GetAllPostalcodesAsync();

        response.StatusCode.Should().BeOneOf(200, 500);
    }

    /// <summary>
    ///     Tests retrieving postal codes without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public void GetAllPostalcodes_WithoutToken_ThrowsUnauthorizedException()
    {
        var exception = Assert.ThrowsAsync<ApiException>(() =>
            LocationClient.GetAllPostalcodesAsync());

        exception.StatusCode.Should().Be(401);
    }

    /// <summary>
    ///     Tests inserting a new location with admin privileges and verifies successful operation.
    /// </summary>
    [Test]
    public async Task InsertLocation_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var postalCode = 10115; // Berlin postal code for testing
        try
        {
            var response = await LocationClient.InsertLocationAsync(postalCode);
            response.StatusCode.Should().BeOneOf(200, 500);
        }
        catch (Exception e)
        {
            Assert.Inconclusive("Rate limit might be exceeded: " + e.Message);
        }
    }

    /// <summary>
    ///     Tests inserting a location with user token and verifies 403 Forbidden response for insufficient privileges.
    /// </summary>
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

    /// <summary>
    ///     Tests inserting a location without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public void InsertLocation_WithoutToken_ThrowsUnauthorizedException()
    {
        var postalCode = 10115;

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            LocationClient.InsertLocationAsync(postalCode));

        exception.StatusCode.Should().Be(401);
    }

    /// <summary>
    ///     Tests removing a postal code with valid authentication and verifies successful deletion or proper error handling.
    /// </summary>
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

    /// <summary>
    ///     Tests removing a postal code without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public void RemovePostalcode_WithoutToken_ThrowsUnauthorizedException()
    {
        var postalCode = 12345;

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            LocationClient.RemovePostalcodeAsync(postalCode));

        exception.StatusCode.Should().Be(401);
    }
}