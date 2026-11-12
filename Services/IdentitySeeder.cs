using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TaskApi.Models;

namespace TaskApi.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["User", "Admin"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            "; ",
                            result.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminRole = await roleManager.FindByNameAsync("Admin")
            ?? throw new InvalidOperationException(
                "Admin role could not be created.");

        var roleClaims = await roleManager.GetClaimsAsync(adminRole);

        if (!roleClaims.Any(c =>
            c.Type == "permission" &&
            c.Value == "tasks.manage.all"))
        {
            await roleManager.AddClaimAsync(
                adminRole,
                new Claim("permission", "tasks.manage.all"));
        }

        var adminEmail = configuration["BootstrapAdmin:Email"];
        var adminPassword = configuration["BootstrapAdmin:Password"];

        // No bootstrap credentials means no initial admin account.
        if (string.IsNullOrWhiteSpace(adminEmail) &&
            string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Both BootstrapAdmin email and password are required.");
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                admin,
                adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        createResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(
                admin,
                "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}