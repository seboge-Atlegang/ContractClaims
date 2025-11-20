using ContractClaims.Models;
using Microsoft.AspNetCore.Identity;

namespace ContractClaims.Seed
{
    public static class SeedData
    {
        // These credentials will be displayed on the login page. You can change them later.
        public static readonly List<(string Role, string Email, string Password, string FirstName, string LastName, decimal? HourlyRate)> SeedUsers =
            new()
            {
                // HR superuser - can manage all
                ("HR","hr@university.local","HrPass123!", "Harriet", "Ramos", null),
                // Lecturer
                ("Lecturer","lecturer1@university.local","LectPass123!","Lindiwe","Mokoena", 250),
                // Program Coordinator
                ("Coordinator","coord@university.local","CoordPass123!","Sipho","Dlamini", null),
                // Manager
                ("Manager","manager@university.local","MgrPass123!","Nomsa","Van",null)
            };

        public static async Task InitializeAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure roles
            var roles = new[] { "HR", "Lecturer", "Coordinator", "Manager" };
            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }

            // Create users
            foreach (var (role, email, password, first, last, hourly) in SeedUsers)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = first,
                        LastName = last,
                        HourlyRate = hourly
                    };
                    var res = await userManager.CreateAsync(user, password);
                    if (!res.Succeeded)
                    {
                        // handle errors if needed (log)
                    }
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            // Make HR a super user by adding to all roles (or you can create policy mapping)
            var hr = await userManager.FindByEmailAsync("hr@university.local");
            if (hr != null)
            {
                foreach (var r in roles)
                {
                    if (!await userManager.IsInRoleAsync(hr, r))
                        await userManager.AddToRoleAsync(hr, r);
                }
            }
        }
    }
}
