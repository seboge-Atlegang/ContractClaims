using ContractClaims.Data;
using ContractClaims.Models;
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

        public HRController(ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // -------------------------------------------------------------
        // HR DASHBOARD
        // -------------------------------------------------------------
        public async Task<IActionResult> Dashboard()
        {
            var allClaims = await _db.Claims
                .Include(c => c.Lecturer)
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            ViewBag.TotalClaims = allClaims.Count;
            ViewBag.PendingClaims = allClaims.Count(c => c.Status == ClaimStatus.Pending);
            ViewBag.ApprovedClaims = allClaims.Count(c => c.Status == ClaimStatus.Approved);

            if (allClaims.Count > 0)
                ViewBag.ApprovalRate = $"{(int)((double)ViewBag.ApprovedClaims / allClaims.Count * 100)}%";
            else
                ViewBag.ApprovalRate = "0%";

            ViewBag.RecentClaims = allClaims.Take(8).ToList();

            var allUsers = await _userManager.Users.ToListAsync();
            return View("Dashboard", allUsers);
        }

        // -------------------------------------------------------------
        // USER LIST
        // -------------------------------------------------------------
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // -------------------------------------------------------------
        // ADD NEW USER (GET)
        // -------------------------------------------------------------
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new List<string> { "Lecturer", "Coordinator", "Manager", "HR" };
            return View();
        }

        // -------------------------------------------------------------
        // ADD NEW USER (POST)
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateUser(string firstName, string lastName, string email, string role, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction("CreateUser");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction("CreateUser");
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            // Add role to user
            await _userManager.AddToRoleAsync(user, role);

            TempData["Success"] = "User account created successfully.";
            return RedirectToAction("Users");
        }

        // -------------------------------------------------------------
        // REPORT PAGE
        // -------------------------------------------------------------
        public IActionResult Reports()
        {
            return View();
        }

        // -------------------------------------------------------------
        // PDF GENERATION
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> GenerateApprovedClaimsPdf(DateTime from, DateTime to)
        {
            var approvedClaims = await _db.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                    c.DateSubmitted.Date >= from.Date &&
                    c.DateSubmitted.Date <= to.Date)
                .ToListAsync();

            if (!approvedClaims.Any())
            {
                TempData["Error"] = "No approved claims found for this date range.";
                return RedirectToAction("Reports");
            }

            string output = "Approved Claims Report\n\n";

            foreach (var claim in approvedClaims)
            {
                output += $"Lecturer: {claim.Lecturer?.Email}\n" +
                          $"Hours: {claim.HoursWorked}\n" +
                          $"Rate: {claim.HourlyRate}\n" +
                          $"Amount: {(claim.HoursWorked * claim.HourlyRate):C}\n" +
                          $"Date: {claim.DateSubmitted}\n\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(output);
            return File(bytes, "text/plain", "ApprovedClaimsReport.txt");
        }
    }
}
