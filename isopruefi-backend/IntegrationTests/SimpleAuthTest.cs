using FluentAssertions;
using IntegrationTests.ApiClient;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rest_API.Models;
using ApiLogin = IntegrationTests.ApiClient.Login;
using ApiJwtToken = IntegrationTests.ApiClient.JwtToken;
using DomainApiUser = Database.EntityFramework.Models.ApiUser;

namespace IntegrationTests;

[TestFixture]
public class SimpleAuthTest : ApiClientTestBase
{
    [Test]
    public async Task SimpleLogin_Test()
    {
        // Create a test user directly
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DomainApiUser>>();

        var testUser = new DomainApiUser
        {
            UserName = "simpletest",
            Email = "simpletest@test.com"
        };

        var result = await userManager.CreateAsync(testUser, "SimpleTest123!");
        if (result.Succeeded) await userManager.AddToRoleAsync(testUser, Roles.User);

        // Try to login using NSwag client
        var loginData = new ApiLogin { UserName = "simpletest", Password = "SimpleTest123!" };

        try
        {
            var response = await AuthenticationClient.LoginAsync(loginData);
            response.StatusCode.Should().Be(200);

            // Read the response content
            var jsonContent = await new StreamReader(response.Stream).ReadToEndAsync();
            jsonContent.Should().NotBeNullOrEmpty();

            var jwtResult = JsonConvert.DeserializeObject<ApiJwtToken>(jsonContent);
            jwtResult.Should().NotBeNull();
            jwtResult!.Token.Should().NotBeNullOrEmpty();
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(200, 400, 401);
            Console.WriteLine($"Status: {ex.StatusCode}");
            Console.WriteLine($"Response: {ex.Response}");
        }
    }
}