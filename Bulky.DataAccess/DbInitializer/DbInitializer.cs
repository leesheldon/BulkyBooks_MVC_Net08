using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DbInitializer;

public class DbInitializer(UserManager<ApplicationUser> userManager, 
    RoleManager<IdentityRole> roleManager, DataContext context) : IDbInitializer
{
    public void Initialize()
    {
        // Apply Migrations if they are not applied.
        try
        {
            if (context.Database.GetPendingMigrations().Count() > 0) {
                context.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
        }
        
        // Create Roles if they are not created.
        if (!roleManager.RoleExistsAsync(StaticDetails.Role_Customer).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Customer)).GetAwaiter().GetResult();
            roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Admin)).GetAwaiter().GetResult();
            roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Employee)).GetAwaiter().GetResult();
            roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Company)).GetAwaiter().GetResult();

            // Create Admin user
            userManager.CreateAsync(new ApplicationUser {
                UserName = "admin@bulkybooks.com",
                Email = "admin@bulkybooks.com",
                Name = "Administrator User",
                PhoneNumber = "07111220099",
                StreetAddress = "16th street Richmond Grove",
                State = "California",
                City = "Sacramento",
                PostalCode = "1100235555"
            }, "Pa$$w0rd").GetAwaiter().GetResult();

            var user = context.ApplicationUsers.FirstOrDefault(x => x.Email == "admin@bulkybooks.com");
            if (user != null) {
                userManager.AddToRoleAsync(user, StaticDetails.Role_Admin).GetAwaiter().GetResult();
            }
        }

        return;
    }
}
