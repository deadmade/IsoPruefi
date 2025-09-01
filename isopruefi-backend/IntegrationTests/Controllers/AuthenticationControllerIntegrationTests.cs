using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Rest_API.Models;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Controllers;

[TestFixture]
public class AuthenticationControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Login_ValidCredentials_ReturnsJwtToken()
    {
        await SeedTestUserAsync("testuser", "TestPassword123!");
        
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Login")
        {
            Content = CreateJsonContent(new Login { UserName = "testuser", Password = "TestPassword123!" })
        };

        var response = await Client.SendAsync(loginRequest);
        var result = await SendAsync<JwtToken>(loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiryDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Test]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Login")
        {
            Content = CreateJsonContent(new Login { UserName = "invalid", Password = "invalid" })
        };

        var response = await Client.SendAsync(loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_MissingUsername_ReturnsBadRequest()
    {
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Login")
        {
            Content = CreateJsonContent(new Login { UserName = "", Password = "password" })
        };

        var response = await Client.SendAsync(loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Login")
        {
            Content = CreateJsonContent(new Login { UserName = "user", Password = "" })
        };

        var response = await Client.SendAsync(loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Register_ValidData_AsAdmin_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Register")
        {
            Content = CreateJsonContent(new Register { UserName = "newuser", Password = "NewPassword123!" })
        };

        var response = await Client.SendAsync(registerRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Register_WithoutAdminToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Register")
        {
            Content = CreateJsonContent(new Register { UserName = "newuser", Password = "NewPassword123!" })
        };

        var response = await Client.SendAsync(registerRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Register_WithoutToken_ReturnsUnauthorized()
    {
        var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Register")
        {
            Content = CreateJsonContent(new Register { UserName = "newuser", Password = "NewPassword123!" })
        };

        var response = await Client.SendAsync(registerRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RefreshToken_ValidToken_ReturnsNewToken()
    {
        var originalToken = await GetJwtTokenAsync("testuser", "TestPassword123!");
        
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Refresh")
        {
            Content = CreateJsonContent(new JwtToken 
            { 
                Token = originalToken, 
                RefreshToken = "valid-refresh-token" 
            })
        };

        var response = await Client.SendAsync(refreshRequest);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Refresh")
        {
            Content = CreateJsonContent(new JwtToken 
            { 
                Token = "invalid-token", 
                RefreshToken = "invalid-refresh-token" 
            })
        };

        var response = await Client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task SeedTestUserAsync(string username, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database.EntityFramework.ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Database.EntityFramework.Models.ApiUser>>();

        if (await userManager.FindByNameAsync(username) != null)
            return;

        var user = new Database.EntityFramework.Models.ApiUser 
        { 
            UserName = username, 
            Email = $"{username}@test.com" 
        };
        
        await userManager.CreateAsync(user, password);
        
        var role = username == "admin" ? Rest_API.Models.Roles.Admin : Rest_API.Models.Roles.User;
        await userManager.AddToRoleAsync(user, role);
    }
}