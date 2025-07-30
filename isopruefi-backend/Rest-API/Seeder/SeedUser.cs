using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Identity;
using Rest_API.Models;

namespace Rest_API.Seeder;

public class SeedUser
{
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

            // Check if any users exist to prevent duplicate seeding
            if (userManager.Users.Any() == false)
            {
                var user = new ApiUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
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

                // Attempt to create admin user
                var createUserResult = await userManager
                    .CreateAsync(user, "Admin@123");

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

                logger.LogInformation("Admin user is created");
            }
        }

        catch (Exception ex)
        {
            logger.LogCritical(ex.Message);
        }
    }
}