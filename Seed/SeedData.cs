using ContractClaims.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ContractClaims.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = new[] { "HR", "Manager", "Coordinator", "Lecturer" };
            foreach (var r in roles)
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));

            // seed users
            async Task CreateIfNotExists(string email, string pwd, string first, string last, string role, decimal? hourly = null)
            {
                var u = await userManager.FindByEmailAsync(email);
                if (u == null)
                {
                    u = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FirstName = first,
                        LastName = last,
                        HourlyRate = hourly
                    };
                    var res = await userManager.CreateAsync(u, pwd);
                    if (!res.Succeeded) throw new Exception($"Failed to create {email}: {string.Join(',', res.Errors)}");
                }
                if (!await userManager.IsInRoleAsync(u, role))
                    await userManager.AddToRoleAsync(u, role);
            }

            await CreateIfNotExists("hr@company.local", "HrPass!23", "Harriet", "Ramos", "HR");
            await CreateIfNotExists("manager@company.local", "MgrPass!23", "Mark", "Manager", "Manager");
            await CreateIfNotExists("coord@company.local", "CoordPass!23", "Claire", "Coord", "Coordinator");
            await CreateIfNotExists("lecturer1@company.local", "LectPass!23", "Liam", "Lecturer", "Lecturer", 350m);
            await CreateIfNotExists("lecturer2@company.local", "Lect2Pass!23", "Laura", "Lecturer", "Lecturer", 300m);

            // give HR all roles (superuser)
            var hr = await userManager.FindByEmailAsync("hr@company.local");
            foreach (var r in roles)
                if (!await userManager.IsInRoleAsync(hr, r))
                    await userManager.AddToRoleAsync(hr, r);
        }
    }
}
