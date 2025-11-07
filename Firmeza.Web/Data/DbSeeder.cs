using Microsoft.AspNetCore.Identity;
using Firmeza.Web.Models;

namespace Firmeza.Web.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            foreach (var r in new[] { "Admin", "Customer" })
                if (!await roles.RoleExistsAsync(r))
                    await roles.CreateAsync(new IdentityRole(r));

            var email = "admin@firmeza.com";
            var admin = await users.FindByEmailAsync(email);
            if (admin is null)
            {
                admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
                var ok = await users.CreateAsync(admin, "Admin123!");
                if (ok.Succeeded) await users.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
