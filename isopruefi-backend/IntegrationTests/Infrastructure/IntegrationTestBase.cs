using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Rest_API.Models;

namespace IntegrationTests.Infrastructure;

[TestFixture]
public abstract class IntegrationTestBase
{
    [OneTimeSetUp]
    public virtual async Task OneTimeSetUp()
    {
        Factory = new IntegrationTestWebApplicationFactory();
        await Factory.StartAsync();
        Client = Factory.CreateClient();
    }

    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }

    [SetUp]
    public virtual void SetUp()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    private static readonly object LockObject = new();
    private static long _counter;

    protected IntegrationTestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected async Task<T?> SendAsync<T>(HttpRequestMessage request)
    {
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(content))
            return default;

        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    protected StringContent CreateJsonContent<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, _jsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

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

    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

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

    protected async Task CleanupDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();

        var testUsers = userManager.Users.Where(u => u.Email.EndsWith("@test.com")).ToList();
        foreach (var user in testUsers) await userManager.DeleteAsync(user);

        await context.SaveChangesAsync();
    }

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