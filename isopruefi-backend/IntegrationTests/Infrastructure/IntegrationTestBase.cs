using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Rest_API.Models;

namespace IntegrationTests.Infrastructure;

/// <summary>
///     Base class for integration tests providing common HTTP client functionality, authentication helpers, and test data
///     management.
/// </summary>
[TestFixture]
public abstract class IntegrationTestBase
{
    /// <summary>
    ///     One-time setup to initialize the test web application factory and HTTP client for all tests in the fixture.
    /// </summary>
    [OneTimeSetUp]
    public virtual async Task OneTimeSetUp()
    {
        Factory = new IntegrationTestWebApplicationFactory();
        await Factory.StartAsync();
        Client = Factory.CreateClient();
    }

    /// <summary>
    ///     One-time cleanup to dispose of the HTTP client and web application factory after all tests complete.
    /// </summary>
    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }

    /// <summary>
    ///     Setup method run before each test to reset the HTTP client authorization header.
    /// </summary>
    [SetUp]
    public virtual void SetUp()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    private static readonly object LockObject = new();
    private static long _counter;

    /// <summary>
    ///     Gets the integration test web application factory instance used for creating test clients.
    /// </summary>
    protected IntegrationTestWebApplicationFactory Factory { get; private set; } = null!;

    /// <summary>
    ///     Gets the HTTP client instance configured for making requests to the test application.
    /// </summary>
    protected HttpClient Client { get; private set; } = null!;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    ///     Sends an HTTP request and deserializes the response content to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response content to.</typeparam>
    /// <param name="request">The HTTP request message to send.</param>
    /// <returns>The deserialized response content or default value if content is empty.</returns>
    protected async Task<T?> SendAsync<T>(HttpRequestMessage request)
    {
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(content))
            return default;

        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    /// <summary>
    ///     Creates JSON content for HTTP requests from the specified object using camelCase property naming.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="obj">The object to serialize to JSON.</param>
    /// <returns>StringContent containing the JSON representation of the object.</returns>
    protected StringContent CreateJsonContent<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    ///     Gets a valid JWT token by creating a test user and performing authentication.
    /// </summary>
    /// <param name="baseUsername">The base username for the test user (default: "admin").</param>
    /// <param name="password">The password for the test user (default: "Admin123!").</param>
    /// <returns>A valid JWT token string for API authentication.</returns>
    protected async Task<string> GetJwtTokenAsync(string baseUsername = "admin", string password = "Admin123!")
    {
        var uniqueUsername = GenerateUniqueUsername(baseUsername);
        await SeedTestUserAsync(uniqueUsername, password, baseUsername);

        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Login")
        {
            Content = CreateJsonContent(new Login { UserName = uniqueUsername, Password = password })
        };

        var response = await Client.SendAsync(loginRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Failed to get JWT token. Status: {response.StatusCode}, Content: {content}");

        var loginResponse = string.IsNullOrEmpty(content)
            ? null
            : JsonSerializer.Deserialize<JwtToken>(content, _jsonOptions);

        return loginResponse?.Token ??
               throw new InvalidOperationException("Failed to deserialize JWT token from response");
    }

    /// <summary>
    ///     Sets the Authorization header on the HTTP client with the provided JWT token.
    /// </summary>
    /// <param name="token">The JWT token to use for authorization.</param>
    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    ///     Creates a test user with the specified credentials and role in the test database.
    /// </summary>
    /// <param name="username">The unique username for the test user.</param>
    /// <param name="password">The password for the test user.</param>
    /// <param name="baseUsername">The base username to determine the user role (default: "admin").</param>
    private async Task SeedTestUserAsync(string username, string password, string baseUsername = "admin")
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure roles exist
        var adminRole = Roles.Admin;
        var userRole = Roles.User;

        if (!await roleManager.RoleExistsAsync(adminRole))
            await roleManager.CreateAsync(new IdentityRole(adminRole));

        if (!await roleManager.RoleExistsAsync(userRole))
            await roleManager.CreateAsync(new IdentityRole(userRole));

        if (await userManager.FindByNameAsync(username) != null)
            return;

        var user = new ApiUser { UserName = username, Email = $"{username}@test.com" };
        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            var role = baseUsername == "admin" ? adminRole : userRole;
            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    ///     Generates a unique username by appending timestamp and counter to the base username to avoid conflicts.
    /// </summary>
    /// <param name="baseUsername">The base username to make unique.</param>
    /// <returns>A unique username string.</returns>
    protected string GenerateUniqueUsername(string baseUsername)
    {
        long counter;
        lock (LockObject)
        {
            counter = ++_counter;
        }

        var timestamp = DateTimeOffset.UtcNow.Ticks;
        return $"{baseUsername}_{timestamp}_{counter}";
    }

    /// <summary>
    ///     Cleans up test data by removing all test users from the database.
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();

        var testUsers = userManager.Users.Where(u => u.Email!.EndsWith("@test.com")).ToList();
        foreach (var user in testUsers) await userManager.DeleteAsync(user);

        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     Creates a test user with the specified credentials and role for use in integration tests.
    /// </summary>
    /// <param name="username">The username for the test user.</param>
    /// <param name="password">The password for the test user.</param>
    /// <param name="role">The role to assign to the test user.</param>
    public async Task CreateTestUserAsync(string username, string password, string role)
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

    /// <summary>
    ///     Authenticates with the specified credentials and returns a valid JWT token for API requests.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <returns>A valid JWT token string.</returns>
    public async Task<string> GetValidJwtTokenAsync(string username, string password)
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