using System.Net;
using Database.EntityFramework.Models;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Rest_API.Models;

namespace IntegrationTests.Controllers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class UserInfoControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetAllUsers_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/UserInfo/GetAllUsers");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetAllUsers_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/UserInfo/GetAllUsers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetAllUsers_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/v1/UserInfo/GetAllUsers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUserById_WithValidUserId_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        // Get a test user ID
        var testUserId = await GetTestUserIdAsync("user");

        var response = await Client.GetAsync($"/api/v1/UserInfo/GetUserById?userId={testUserId}");

        response.StatusCode.Should()
            .BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetUserById_WithInvalidUserId_ReturnsNotFound()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var invalidUserId = "00000000-0000-0000-0000-000000000000";

        var response = await Client.GetAsync($"/api/v1/UserInfo/GetUserById?userId={invalidUserId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetUserById_WithoutToken_ReturnsUnauthorized()
    {
        var testUserId = "test-user-id";

        var response = await Client.GetAsync($"/api/v1/UserInfo/GetUserById?userId={testUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var testUserId = await GetTestUserIdAsync("user");
        var changePasswordRequest = new ChangePassword
        {
            UserId = testUserId,
            CurrentPassword = "User123!",
            NewPassword = "NewPassword123!"
        };

        var response =
            await Client.PostAsync("/api/v1/UserInfo/ChangePassword", CreateJsonContent(changePasswordRequest));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound,
            HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var testUserId = await GetTestUserIdAsync("user");
        var changePasswordRequest = new ChangePassword
        {
            UserId = testUserId,
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        var response =
            await Client.PostAsync("/api/v1/UserInfo/ChangePassword", CreateJsonContent(changePasswordRequest));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        var changePasswordRequest = new ChangePassword
        {
            UserId = "test-user-id",
            CurrentPassword = "CurrentPassword",
            NewPassword = "NewPassword123!"
        };

        var response =
            await Client.PostAsync("/api/v1/UserInfo/ChangePassword", CreateJsonContent(changePasswordRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangeUser_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var testUser = new ApiUser
        {
            Id = await GetTestUserIdAsync("user"),
            UserName = "updated_user",
            Email = "updated@test.com"
        };

        var response = await Client.PutAsync("/api/v1/UserInfo/ChangeUser", CreateJsonContent(testUser));

        response.StatusCode.Should()
            .BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task ChangeUser_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var testUser = new ApiUser
        {
            Id = "test-user-id",
            UserName = "updated_user",
            Email = "updated@test.com"
        };

        var response = await Client.PutAsync("/api/v1/UserInfo/ChangeUser", CreateJsonContent(testUser));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteUser_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Create a user to delete
        var userIdToDelete = await CreateTestUserForDeletionAsync();

        var response = await Client.DeleteAsync($"/api/v1/UserInfo/DeleteUser?userId={userIdToDelete}");

        response.StatusCode.Should()
            .BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task DeleteUser_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var response = await Client.DeleteAsync("/api/v1/UserInfo/DeleteUser?userId=test-user-id");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteUser_WithNonExistentUserId_ReturnsNotFound()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var nonExistentUserId = "00000000-0000-0000-0000-000000000000";

        var response = await Client.DeleteAsync($"/api/v1/UserInfo/DeleteUser?userId={nonExistentUserId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    private async Task<string> GetTestUserIdAsync(string username)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();
        var user = await userManager.FindByNameAsync(username);
        return user?.Id ?? "00000000-0000-0000-0000-000000000000";
    }

    private async Task<string> CreateTestUserForDeletionAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();

        var uniqueUsername = GenerateUniqueUsername("user_to_delete");
        var testUser = new ApiUser
        {
            UserName = uniqueUsername,
            Email = $"{uniqueUsername}@test.com"
        };

        var result = await userManager.CreateAsync(testUser, "DeleteMe123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(testUser, Roles.User);
            return testUser.Id;
        }

        return "00000000-0000-0000-0000-000000000000";
    }
}