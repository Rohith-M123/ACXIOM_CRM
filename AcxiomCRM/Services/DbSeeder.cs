using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AcxiomCRM.Services;

/// <summary>
/// Seeds the database with required roles and the initial administrator account.
/// This is idempotent — safe to run on every application startup.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Role names for AcxiomCRM.
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string SalesExecutive = "Sales Executive";

        public static readonly string[] All = [Admin, Manager, SalesExecutive];
    }

    /// <summary>
    /// Seeds roles and the initial admin user. Call from Program.cs after app.Build().
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // --- Seed Roles ---
            foreach (var roleName in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                        logger.LogInformation("Created role: {Role}", roleName);
                    else
                        logger.LogError("Failed to create role {Role}: {Errors}", roleName,
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // --- Seed Admin User ---
            var adminEmail = config["SeedAdmin:Email"];
            var adminPassword = config["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning(
                    "SeedAdmin:Email or SeedAdmin:Password is not configured. " +
                    "Add them to appsettings.Development.json to create the initial administrator.");
                return;
            }

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                    logger.LogInformation("Admin account seeded for: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create admin account: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Ensure existing admin has the Admin role (in case it was lost)
                if (!await userManager.IsInRoleAsync(existingAdmin, Roles.Admin))
                {
                    await userManager.AddToRoleAsync(existingAdmin, Roles.Admin);
                    logger.LogInformation("Re-assigned Admin role to: {Email}", adminEmail);
                }
                logger.LogInformation("Admin account already exists for: {Email}", adminEmail);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "An error occurred during database seeding. " +
                "Ensure the database is created and migrations have been applied.");
            throw; // Re-throw so the startup fails visibly rather than silently.
        }
    }
}
