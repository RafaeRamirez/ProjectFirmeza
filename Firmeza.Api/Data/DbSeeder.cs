using Firmeza.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var requiredRoles = new[] { "SuperAdmin", "Admin", "Cliente" };
        foreach (var role in requiredRoles)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@firmeza.com";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";
        var adminUser = await users.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(",", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear el usuario administrador: {errors}");
            }
        }

        await EnsureRoleAsync(users, adminUser, "SuperAdmin");
        await EnsureRoleAsync(users, adminUser, "Admin");
    }

    private static async Task EnsureRoleAsync(UserManager<AppUser> users, AppUser user, string role)
    {
        if (!await users.IsInRoleAsync(user, role))
        {
            await users.AddToRoleAsync(user, role);
        }
    }
}
