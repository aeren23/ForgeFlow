using ForgeFlow.Identity.Domain.Entities;
using ForgeFlow.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Identity.Infrastructure.Services;

/// <summary>
/// Varsayılan rolleri ve admin kullanıcıyı oluşturan seeder.
/// Uygulama başlangıcında çalıştırılır.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();

        // Varsayılan rolleri oluştur
        foreach (var (roleName, description) in SystemRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName, description, isSystem: true);
                var result = await roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    logger.LogInformation("Created system role: {RoleName}", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Varsayılan admin kullanıcı oluştur (eğer yoksa)
        var adminEmail = "admin@forgeflow.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true // Admin için email doğrulaması atlanır
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, SystemRoles.Admin);
                logger.LogInformation("Created default admin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure password is correct even if user exists
            var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            var result = await userManager.ResetPasswordAsync(adminUser, token, "Admin123!");
            if (result.Succeeded)
            {
                logger.LogInformation("Reset password for default admin user: {Email}", adminEmail);
            }

            // Ensure properties are correct
            if (!adminUser.IsSystemAdmin || !adminUser.IsActive)
            {
                adminUser.IsSystemAdmin = true;
                adminUser.IsActive = true;
                await userManager.UpdateAsync(adminUser);
            }
        }

        // Fix for existing users: Set IsActive = true if false (migration-like step)
        var inactiveUsers = await userManager.Users.Where(u => !u.IsActive).ToListAsync();
        if (inactiveUsers.Any())
        {
            logger.LogInformation("Found {Count} inactive users. Activating them...", inactiveUsers.Count);
            foreach (var user in inactiveUsers)
            {
                user.IsActive = true;
                await userManager.UpdateAsync(user);
            }
            logger.LogInformation("Activated {Count} users.", inactiveUsers.Count);
        }


    }
}
