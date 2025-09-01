using System.Net.Http.Headers;
using FluentAssertions;
using IntegrationTests.ApiClient;
using IntegrationTests.Infrastructure;
using Newtonsoft.Json;
using Rest_API.Models;
using ApiLogin = IntegrationTests.ApiClient.Login;
using ApiJwtToken = IntegrationTests.ApiClient.JwtToken;
using ApiRegister = IntegrationTests.ApiClient.Register;

namespace IntegrationTests.Controllers;

[TestFixture]
public class AuthenticationControllerIntegrationTests : ApiClientTestBase
{
    [Test]
    public async Task Login_ValidCredentials_ReturnsJwtToken()
    {
        // Arrange: Create test user
        await CreateTestUserAsync("testuser", "TestPassword123!", Roles.User);

        var loginData = new ApiLogin { UserName = "testuser", Password = "TestPassword123!" };

        // Act
        var response = await AuthenticationClient.LoginAsync(loginData);

        // Assert
        response.StatusCode.Should().Be(200);

        // Read the response content
        var jsonContent = await new StreamReader(response.Stream).ReadToEndAsync();
        jsonContent.Should().NotBeNullOrEmpty();

        var result = JsonConvert.DeserializeObject<ApiJwtToken>(jsonContent);
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Login_InvalidCredentials_ThrowsApiException()
    {
        // Arrange
        var loginData = new ApiLogin { UserName = "invalid", Password = "invalid" };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ApiException>(() =>
            AuthenticationClient.LoginAsync(loginData));

        exception.StatusCode.Should().Be(401);
    }

    [Test]
    public async Task Register_WithAdminToken_ReturnsOk()
    {
        // Arrange: Create admin user and get token
        await CreateTestUserAsync("admin", "Admin123!", Roles.Admin);
        var token = await GetValidJwtTokenAsync("admin", "Admin123!");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var registerData = new ApiRegister { UserName = "newuser", Password = "NewPassword123!" };

        // Act
        await AuthenticationClient.RegisterAsync(registerData);

        // Assert - No exception means success (void return)
        Assert.Pass();
    }
}