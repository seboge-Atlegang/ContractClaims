using ContractClaims.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContractClaims.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly UserManager<ApplicationUser> _users;

        public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
        {
            _signIn = signIn;
            _users = users;
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.Seed = new[]
            {
                new { Role="HR", Email="hr@company.local", Password="HrPass!23"},
                new { Role="Manager", Email="manager@company.local", Password="MgrPass!23"},
                new { Role="Coordinator", Email="coord@company.local", Password="CoordPass!23"},
                new { Role="Lecturer", Email="lecturer1@company.local", Password="LectPass!23"}
            };
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            var res = await _signIn.PasswordSignInAsync(Email, Password, isPersistent: false, lockoutOnFailure: false);
            if (res.Succeeded)
            {
                var u = await _users.FindByEmailAsync(Email);
                if (await _users.IsInRoleAsync(u, "HR")) return RedirectToAction("Dashboard", "HR");
                if (await _users.IsInRoleAsync(u, "Manager")) return RedirectToAction("Index", "Coordinator");
                if (await _users.IsInRoleAsync(u, "Coordinator")) return RedirectToAction("Index", "Coordinator");
                if (await _users.IsInRoleAsync(u, "Lecturer")) return RedirectToAction("Index", "Claims");
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Invalid credentials");
            ViewBag.Seed = new[] { new { Role = "HR", Email = "hr@company.local", Password = "HrPass!23" } };
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
