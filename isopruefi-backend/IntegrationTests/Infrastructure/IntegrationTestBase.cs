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
    protected IntegrationTestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

    protected async Task<string> GetJwtTokenAsync(string username = "admin", string password = "Admin123!")
    {
        await SeedTestUserAsync(username, password);
        
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/Authentication/Login")
        {
            Content = CreateJsonContent(new Login { UserName = username, Password = password })
        };

        var response = await Client.SendAsync(loginRequest);
        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<JwtToken>(content, _jsonOptions);
        
        return loginResponse?.Token ?? throw new InvalidOperationException("Failed to get JWT token");
    }

    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task SeedTestUserAsync(string username, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure roles exist
        var adminRole = Rest_API.Models.Roles.Admin;
        var userRole = Rest_API.Models.Roles.User;
        
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
            var role = username == "admin" ? adminRole : userRole;
            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    protected async Task CleanupDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        context.Users.RemoveRange(context.Users);
        context.UserRoles.RemoveRange(context.UserRoles);
        await context.SaveChangesAsync();
    }
}