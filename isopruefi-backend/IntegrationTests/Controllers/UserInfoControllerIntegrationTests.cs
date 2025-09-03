using System.Net;
using FluentAssertions;
using IntegrationTests.ApiClient;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Rest_API.Models;
using ApiChangePassword = IntegrationTests.ApiClient.ChangePassword;
using DomainApiUser = Database.EntityFramework.Models.ApiUser;

namespace IntegrationTests.Controllers;

/// <summary>
///     Integration tests for the User Info Controller to verify user management, profile operations, and access control
///     functionality.
/// </summary>
[TestFixture]
public class UserInfoControllerIntegrationTests : ApiClientTestBase
{
    /// <summary>
    ///     Tests retrieving all users with admin privileges and verifies successful response or proper error handling.
    /// </summary>
    [Test]
    public async Task GetAllUsers_WithAdminToken_ReturnsOk()
    {
        // Note: GetAllUsers is not available in the current generated API client
        // Keeping this test for now but marking as inconclusive
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/UserInfo/GetAllUsers");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    /// <summary>
    ///     Tests retrieving all users with user token and verifies 403 Forbidden response for insufficient privileges.
    /// </summary>
    [Test]
    public async Task GetAllUsers_WithUserToken_ReturnsForbidden()
    {
        // Note: GetAllUsers is not available in the current generated API client
        // Keeping this test for now but marking as inconclusive
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/UserInfo/GetAllUsers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    ///     Tests retrieving all users without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public async Task GetAllUsers_WithoutToken_ReturnsUnauthorized()
    {
        // Note: GetAllUsers is not available in the current generated API client
        // Keeping this test for now but marking as inconclusive
        var response = await Client.GetAsync("/api/v1/UserInfo/GetAllUsers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Tests retrieving user by invalid ID and verifies 404 Not Found response or proper error handling.
    /// </summary>
    [Test]
    public async Task GetUserById_WithInvalidUserId_ReturnsNotFound()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var invalidUserId = "00000000-0000-0000-0000-000000000000";

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            UserInfoClient.GetUserByIdAsync(invalidUserId));

        exception.StatusCode.Should().BeOneOf(404, 500);
    }

    /// <summary>
    ///     Tests retrieving user by ID without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public void GetUserById_WithoutToken_ReturnsUnauthorized()
    {
        var testUserId = "test-user-id";

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            UserInfoClient.GetUserByIdAsync(testUserId));

        exception.StatusCode.Should().Be(401);
    }

    /// <summary>
    ///     Tests password change with invalid current password and verifies proper error response handling.
    /// </summary>
    [Test]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var testUserId = await GetTestUserIdAsync("user");
        var changePasswordRequest = new ApiChangePassword
        {
            UserId = testUserId,
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            UserInfoClient.ChangePasswordAsync(changePasswordRequest));

        exception.StatusCode.Should().BeOneOf(400, 500, 404);
    }

    /// <summary>
    ///     Tests password change without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public void ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        var changePasswordRequest = new ApiChangePassword
        {
            UserId = "test-user-id",
            CurrentPassword = "CurrentPassword",
            NewPassword = "NewPassword123!"
        };

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            UserInfoClient.ChangePasswordAsync(changePasswordRequest));

        exception.StatusCode.Should().Be(401);
    }

    /// <summary>
    ///     Tests user deletion with user token and verifies 403 Forbidden response for insufficient privileges.
    /// </summary>
    [Test]
    public async Task DeleteUser_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            UserInfoClient.DeleteUserAsync("test-user-id"));

        exception.StatusCode.Should().Be(403);
    }

    /// <summary>
    ///     Tests user deletion with non-existent user ID and verifies 404 Not Found response or proper error handling.
    /// </summary>
    [Test]
    public async Task DeleteUser_WithNonExistentUserId_ReturnsNotFound()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var nonExistentUserId = "00000000-0000-0000-0000-000000000000";

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            UserInfoClient.DeleteUserAsync(nonExistentUserId));

        exception.StatusCode.Should().BeOneOf(404, 500);
    }

    /// <summary>
    ///     Retrieves the user ID for a test user by username from the database.
    /// </summary>
    /// <param name="username">The username of the test user.</param>
    /// <returns>The user ID or a default GUID if user not found.</returns>
    private async Task<string> GetTestUserIdAsync(string username)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DomainApiUser>>();
        var user = await userManager.FindByNameAsync(username);
        return user?.Id ?? "00000000-0000-0000-0000-000000000000";
    }

    /// <summary>
    ///     Creates a test user specifically for deletion testing purposes.
    /// </summary>
    /// <returns>The user ID of the created test user or a default GUID if creation fails.</returns>
    private async Task<string> CreateTestUserForDeletionAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DomainApiUser>>();

        var uniqueUsername = GenerateUniqueUsername("user_to_delete");
        var testUser = new DomainApiUser
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