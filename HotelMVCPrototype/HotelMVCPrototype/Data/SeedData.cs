using HotelMVCPrototype.Models;
using Microsoft.AspNetCore.Identity;

namespace HotelMVCPrototype.Data
{
    public static class SeedData
    {
        public static async Task SeedRolesAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Admin", "Reception", "Housekeeping", "Maintenance", "Bar","Security" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            string adminEmail = "admin@hotel.com";
            string password = "Admin123!";

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, password);
                if (!result.Succeeded)
                    throw new Exception("Failed to create admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));

                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        public static async Task SeedMenuItemsAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            if (context.MenuItems.Any())
                return;

            context.MenuItems.AddRange(
                new MenuItem { Name = "Coffee", Price = 3.00m },
                new MenuItem { Name = "Beer", Price = 5.00m },
                new MenuItem { Name = "Water", Price = 2.00m },
                new MenuItem { Name = "Whiskey", Price = 7.50m }
            );

            await context.SaveChangesAsync();
        }
    }
}
