using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Database.EntityFramework.Models;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Rest_API.Models;

namespace IntegrationTests.Controllers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class AuthenticationControllerIntegrationTests : IntegrationTestBase
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task Login_ValidCredentials_ReturnsJwtToken()
    {
        // Arrange: Create test user
        var uniqueUsername = GenerateUniqueUsername("testuser");
        await CreateTestUserAsync(uniqueUsername, "TestPassword123!", Roles.User);

        var loginData = new Login { UserName = uniqueUsername, Password = "TestPassword123!" };
        var json = JsonSerializer.Serialize(loginData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1/Authentication/Login", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().NotBeNullOrEmpty();

        var result = JsonSerializer.Deserialize<JwtToken>(responseContent, _jsonOptions);
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginData = new Login { UserName = "invalid", Password = "invalid" };
        var json = JsonSerializer.Serialize(loginData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1/Authentication/Login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Register_WithAdminToken_ReturnsOk()
    {
        // Arrange: Create admin user and get token
        var uniqueAdminUsername = GenerateUniqueUsername("admin");
        await CreateTestUserAsync(uniqueAdminUsername, "Admin123!", Roles.Admin);
        var token = await GetValidJwtTokenAsync(uniqueAdminUsername, "Admin123!");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueNewUsername = GenerateUniqueUsername("newuser");
        var registerData = new Register { UserName = uniqueNewUsername, Password = "NewPassword123!" };
        var json = JsonSerializer.Serialize(registerData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1/Authentication/Register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Register_WithoutAdminToken_ReturnsForbiddenOrUnauthorized()
    {
        // Arrange: Create regular user and get token
        var uniqueUsername = GenerateUniqueUsername("user");
        await CreateTestUserAsync(uniqueUsername, "User123!", Roles.User);
        var token = await GetValidJwtTokenAsync(uniqueUsername, "User123!");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueNewUsername = GenerateUniqueUsername("newuser2");
        var registerData = new Register { UserName = uniqueNewUsername, Password = "NewPassword123!" };
        var json = JsonSerializer.Serialize(registerData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1/Authentication/Register", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    private async Task CreateTestUserAsync(string username, string password, string role)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();

        // Check if user already exists
        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser != null) return;

        var user = new ApiUser
        {
            UserName = username,
            Email = $"{username}@test.com"
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded) await userManager.AddToRoleAsync(user, role);
    }

    private async Task<string> GetValidJwtTokenAsync(string username, string password)
    {
        var loginData = new Login { UserName = username, Password = password };
        var json = JsonSerializer.Serialize(loginData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/v1/Authentication/Login", content);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to login: {response.StatusCode}, {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JwtToken>(responseContent, _jsonOptions);

        return tokenResponse?.Token ?? throw new InvalidOperationException("No token received");
    }
}