using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity;
using Rest_API.Models;

namespace Rest_API.Seeder;

/// <summary>
/// Seeds the initial user data into the application.
/// </summary>
public class SeedUser
{
    /// <summary>
    /// Seeds the initial user data into the application.
    /// </summary>
    /// <param name="app"></param>
    public static async Task SeedData(IApplicationBuilder app)
    {
        // Create a scoped service provider to resolve dependencies
        using var scope = app.ApplicationServices.CreateScope();

        // resolve the logger service
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedUser>>();

        try
        {
            // resolve other dependencies
            var userManager = scope.ServiceProvider.GetService<UserManager<ApiUser>>();
            var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Get admin credentials from configuration
            var adminUserName = configuration["Admin:UserName"];
            var adminEmail = configuration["Admin:Email"];
            var adminPassword = configuration["Admin:Password"];

            // Validate required admin configuration
            if (string.IsNullOrWhiteSpace(adminUserName))
            {
                logger.LogError("Admin:UserName is not configured. Please set the Admin:UserName environment variable");
                throw new InvalidOperationException("Admin:UserName configuration is required");
            }

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                logger.LogError("Admin:Email is not configured. Please set the Admin:Email environment variable");
                throw new InvalidOperationException("Admin:Email configuration is required");
            }

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogError("Admin:Password is not configured. Please set the Admin:Password environment variable");
                throw new InvalidOperationException("Admin:Password configuration is required");
            }

            logger.LogInformation("Using admin configuration - UserName: {AdminUserName}", adminUserName);

            // Check if any users exist to prevent duplicate seeding
            if (userManager.Users.Any() == false)
            {
                var user = new ApiUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                // Create Admin role if it doesn't exist
                if (await roleManager.RoleExistsAsync(Roles.Admin) == false)
                {
                    logger.LogInformation("Admin role is creating");
                    var roleResult = await roleManager
                        .CreateAsync(new IdentityRole(Roles.Admin));

                    if (roleResult.Succeeded == false)
                    {
                        var roleErros = roleResult.Errors.Select(e => e.Description);
                        logger.LogError("Failed to create admin role. Errors : {Join}", string.Join(",", roleErros));

                        return;
                    }

                    logger.LogInformation("Admin role is created");
                }

                // Create User role if it doesn't exist
                if (await roleManager.RoleExistsAsync(Roles.User) == false)
                {
                    logger.LogInformation("User role is creating");
                    var userRoleResult = await roleManager
                        .CreateAsync(new IdentityRole(Roles.User));

                    if (userRoleResult.Succeeded == false)
                    {
                        var userRoleErrors = userRoleResult.Errors.Select(e => e.Description);
                        logger.LogError("Failed to create user role. Errors : {Join}",
                            string.Join(",", userRoleErrors));

                        return;
                    }

                    logger.LogInformation("User role is created");
                }

                // Attempt to create admin user
                var createUserResult = await userManager
                    .CreateAsync(user, adminPassword);
                // Validate user creation
                if (createUserResult.Succeeded == false)
                {
                    var errors = createUserResult.Errors.Select(e => e.Description);
                    logger.LogError("Failed to create admin user. Errors: {Join}", string.Join(", ", errors));
                    return;
                }

                // adding role to user
                var addUserToRoleResult = await userManager
                    .AddToRoleAsync(user, Roles.Admin);

                if (addUserToRoleResult.Succeeded == false)
                {
                    var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                    logger.LogError("Failed to add admin role to user. Errors : {Join}", string.Join(",", errors));
                }

                addUserToRoleResult = await userManager
                    .AddToRoleAsync(user, Roles.User);

                if (addUserToRoleResult.Succeeded == false)
                {
                    var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                    logger.LogError("Failed to add admin role to user. Errors : {Join}", string.Join(",", errors));
                }

                logger.LogInformation("Admin user is created");
            }
        }

        catch (Exception ex)
        {
            logger.LogCritical(ex.Message);
        }
    }
}