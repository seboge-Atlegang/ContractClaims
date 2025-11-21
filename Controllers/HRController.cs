using ContractClaims.Data;
using ContractClaims.Models;
using ContractClaims.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractClaims.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly QuestPdfReportBuilder _pdfBuilder;
        private readonly IWebHostEnvironment _env;

        public HRController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            QuestPdfReportBuilder pdfBuilder,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _pdfBuilder = pdfBuilder;
            _env = env;
        }

        // ====================
        // HR Dashboard
        // ====================
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalClaims = await _db.Claims.CountAsync();
            ViewBag.PendingClaims = await _db.Claims.CountAsync(c => c.Status == ClaimStatus.Pending);
            ViewBag.ApprovedClaims = await _db.Claims.CountAsync(c => c.Status == ClaimStatus.Approved);

            ViewBag.RecentClaims = await _db.Claims
                .Include(c => c.Lecturer)
                .OrderByDescending(c => c.DateSubmitted)
                .Take(8)
                .ToListAsync();

            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            return View(users);
        }

        // ====================
        // Users List
        // ====================
        public async Task<IActionResult> Users(string q = null)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(u => u.Email.Contains(q)
                    || u.FirstName.Contains(q)
                    || u.LastName.Contains(q));

            var users = await query.OrderBy(u => u.Email).ToListAsync();
            return View(users);
        }

        // ====================
        // Create User (GET)
        // ====================
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new List<string> { "Lecturer", "Coordinator", "Manager", "HR" };
            return View();
        }

        // ====================
        // Create User (POST)
        // ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserVM model)
        {
            ViewBag.Roles = new List<string> { "Lecturer", "Coordinator", "Manager", "HR" };

            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                HourlyRate = model.HourlyRate
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            // Assign role
            var addRole = await _userManager.AddToRoleAsync(user, model.Role);

            if (!addRole.Succeeded)
            {
                foreach (var error in addRole.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            TempData["Success"] = "User created successfully!";
            return RedirectToAction(nameof(Users));
        }

        // ====================
        // Edit User
        // ====================
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new EditUserVM
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                HourlyRate = user.HourlyRate
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.HourlyRate = model.HourlyRate;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            TempData["Success"] = "User updated successfully!";
            return RedirectToAction(nameof(Users));
        }


        // ====================
        // Delete User
        // ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);

            TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Users));
        }

        // ====================
        // Reports
        // ====================
        public IActionResult Reports() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateApprovedClaimsPdf(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1).AddTicks(-1);

            var claims = await _db.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                            c.DateSubmitted >= start &&
                            c.DateSubmitted <= end)
                .ToListAsync();

            if (!claims.Any())
            {
                TempData["Error"] = "No approved claims in this date range.";
                return RedirectToAction(nameof(Reports));
            }

            // FIXED: load logo from wwwroot/images
            string logoPath = Path.Combine(_env.WebRootPath, "images/logo.png");

            var pdf = _pdfBuilder.BuildApprovedClaimsReport(claims, start, end, logoPath);

            return File(pdf, "application/pdf", $"ApprovedClaims_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf");
        }
    }
}
