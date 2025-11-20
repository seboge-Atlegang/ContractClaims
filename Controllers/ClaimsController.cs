using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContractClaims.Controllers
{
    [Authorize(Roles = "Lecturer,HR")]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ClaimsController(ApplicationDbContext db, UserManager<ApplicationUser> um, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = um;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = _db.LecturerClaims.Where(c => c.LecturerId == user.Id).OrderByDescending(c => c.DateSubmitted).ToList();
            return View(claims);
        }

        public IActionResult Create()
        {
            return View(new LecturerClaim());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LecturerClaim model, IFormFile upload)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!ModelState.IsValid) return View(model);

            model.LecturerId = user.Id;
            model.DateSubmitted = DateTime.UtcNow;

            // File upload handling
            if (upload != null && upload.Length > 0)
            {
                var allowed = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".xls" };
                var ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("", "File type not allowed");
                    return View(model);
                }
                if (upload.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "File too large (max 5MB)");
                    return View(model);
                }

                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsRoot);
                var safeName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRoot, safeName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await upload.CopyToAsync(stream);
                }
                model.DocumentPath = $"/uploads/{safeName}";
            }

            _db.LecturerClaims.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var claim = await _db.LecturerClaims.FindAsync(id);
            if (claim == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (claim.LecturerId != user.Id && !(await _userManager.IsInRoleAsync(user, "HR")))
                return Forbid();
            return View(claim);
        }
    }
}
