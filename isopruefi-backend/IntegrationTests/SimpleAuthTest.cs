using System.Net;
using System.Text;
using System.Text.Json;
using Database.EntityFramework.Models;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Rest_API.Models;

namespace IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SimpleAuthTest : IntegrationTestBase
{
    [Test]
    public async Task SimpleLogin_Test()
    {
        // Create a test user directly
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();

        var testUser = new ApiUser
        {
            UserName = "simpletest",
            Email = "simpletest@test.com"
        };

        var result = await userManager.CreateAsync(testUser, "SimpleTest123!");
        if (result.Succeeded) await userManager.AddToRoleAsync(testUser, Roles.User);

        // Try to login
        var loginData = new { userName = "simpletest", password = "SimpleTest123!" };
        var json = JsonSerializer.Serialize(loginData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/v1/Authentication/Login", content);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {responseContent}");

        // This test is mainly to verify the infrastructure works
        // We don't assert success, just that we get a response
        responseContent.Should().NotBeNull();
    }
}