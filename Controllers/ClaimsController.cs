using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractClaims.Controllers
{
    [Authorize(Roles = "Lecturer,HR")]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ClaimsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        // Lecturer + HR: list claims for this user (HR can view all via HR dashboard)
        [Authorize(Roles = "Lecturer,HR")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("HR"))
            {
                // HR: show all claims (or optionally filter)
                var all = await _db.Claims.Include(c => c.Lecturer).OrderByDescending(c => c.DateSubmitted).ToListAsync();
                return View("Index", all);
            }
            else
            {
                var mine = await _db.Claims.Include(c => c.Lecturer)
                    .Where(c => c.LecturerId == user.Id)
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();
                return View("Index", mine);
            }
        }

        // GET: Create form
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var vm = new Claim();
            if (user?.HourlyRate != null) vm.HourlyRate = user.HourlyRate.Value;
            return View(vm);
        }

        // POST: Create claim (Updated for file error handling and redirect fix)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim model, IFormFile upload)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Unable to determine the logged in user.";
                return RedirectToAction("Create");
            }

            // server-side validation
            if (model.HoursWorked <= 0)
                ModelState.AddModelError(nameof(model.HoursWorked), "Please enter hours worked (> 0).");
            if (model.HourlyRate <= 0)
                ModelState.AddModelError(nameof(model.HourlyRate), "Please enter hourly rate (> 0).");

            string ext = null;

            // File validation logic
            if (upload != null && upload.Length > 0)
            {
                var allowed = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".xls" };
                ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("upload", "File type not allowed. Use pdf/doc/docx/xlsx/xls.");
                }
                if (upload.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("upload", "File too large (max 5MB).");
                }
            }

            if (!ModelState.IsValid) return View(model);

            model.LecturerId = user.Id;
            model.DateSubmitted = DateTime.UtcNow;
            model.Status = ClaimStatus.Pending;

            try
            {
                // File Upload Logic
                if (upload != null && upload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await upload.CopyToAsync(stream);
                    }
                    model.DocumentPath = $"/uploads/{fileName}";
                }

                // Database Save Logic
                _db.Claims.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Claim submitted successfully.";

                // Corrected redirect logic
                if (User.IsInRole("HR")) return RedirectToAction("Dashboard", "HR");
                return RedirectToAction("Index", "Claims");
            }
            catch (System.IO.IOException ex)
            {
                // Catches file system errors (e.g., lack of write permission)
                ModelState.AddModelError("", $"File Save Error: Check if the application has write permission to 'wwwroot/uploads'. Details: {ex.Message}");
                return View(model);
            }
            catch (Exception ex)
            {
                // Catches database or other unexpected errors
                ModelState.AddModelError("", $"Submission Error: An unexpected error occurred. Details: {ex.Message}");
                return View(model);
            }
        }

        // GET details (accessible to Lecturer who owns it and HR)
        [Authorize(Roles = "Lecturer,HR")]
        public async Task<IActionResult> Details(int id)
        {
            var claim = await _db.Claims.Include(c => c.Lecturer).FirstOrDefaultAsync(c => c.Id == id);
            if (claim == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("HR") && claim.LecturerId != user.Id)
                return Forbid();

            return View(claim);
        }
    }
}