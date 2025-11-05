using System.Threading.Tasks;
using Firmeza.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Firmeza.Web.Data
{
    // Creates roles (Admin, Customer) and a default admin account if not present
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            // Create roles if they do not exist
            var roles = new[] { "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Default admin (DEV ONLY). Change later to secure credentials.
            const string adminEmail = "admin@firmeza.local";
            const string adminPass  = "Admin123$";

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new AppUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var create = await userManager.CreateAsync(admin, adminPass);
                if (create.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
                // else: handle errors if needed (logging)
            }
        }
    }
}
