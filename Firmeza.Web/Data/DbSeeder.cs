using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Models;

namespace Firmeza.Web.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await EnsureChatBotConfigAsync(db);

            var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            foreach (var r in new[] { "SuperAdmin", "Admin", "Customer" })
                if (!await roles.RoleExistsAsync(r))
                    await roles.CreateAsync(new IdentityRole(r));

            foreach (var obsolete in new[] { "Cliente" })
            {
                var role = await roles.FindByNameAsync(obsolete);
                if (role is null) continue;

                var allUsers = await users.Users.ToListAsync();
                foreach (var user in allUsers)
                {
                    if (await users.IsInRoleAsync(user, obsolete))
                        await users.RemoveFromRoleAsync(user, obsolete);
                }

                await roles.DeleteAsync(role);
            }

            var email = "admin@firmeza.com";
            var admin = await users.FindByEmailAsync(email);
            if (admin is null)
            {
                admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
                var ok = await users.CreateAsync(admin, "Admin123!");
                if (ok.Succeeded)
                {
                    await users.AddToRoleAsync(admin, "SuperAdmin");
                    await users.AddToRoleAsync(admin, "Admin");
                }
            }
            else
            {
                if (!await users.IsInRoleAsync(admin, "SuperAdmin"))
                    await users.AddToRoleAsync(admin, "SuperAdmin");
                if (!await users.IsInRoleAsync(admin, "Admin"))
                    await users.AddToRoleAsync(admin, "Admin");
            }
        }

        private static async Task EnsureChatBotConfigAsync(AppDbContext db)
        {
            if (!await db.ChatBotSettings.AnyAsync())
            {
                db.ChatBotSettings.Add(new ChatBotSettings
                {
                    ApiKey = string.Empty,
                    Model = "models/gemini-1.5-flash",
                    Scope = "https://www.googleapis.com/auth/cloud-platform",
                    ServiceAccountJsonPath = string.Empty,
                    Endpoint = "https://generativelanguage.googleapis.com",
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
