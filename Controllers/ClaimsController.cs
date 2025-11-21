using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

        // ------------------------- INDEX -------------------------
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("HR"))
            {
                var allClaims = await _db.Claims
                    .Include(c => c.Lecturer)
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                return View(allClaims);
            }

            var myClaims = await _db.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.LecturerId == user.Id)
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            return View(myClaims);
        }

        // ------------------------- CREATE: GET -------------------------
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            return View(new Claim
            {
                HourlyRate = user.HourlyRate ?? 0
            });
        }

        // ------------------------- CREATE: POST -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim model, IFormFile upload)
        {
            var user = await _userManager.GetUserAsync(User);

            // IMPORTANT: MUST SET BEFORE VALIDATION
            model.LecturerId = user.Id;

            // Fix SA decimal formats (e.g., 350,00)
            if (Request.Form.TryGetValue("HourlyRate", out var rateValue))
            {
                string cleaned = rateValue.ToString().Replace(",", ".");
                if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRate))
                    model.HourlyRate = parsedRate;
            }

            if (model.HoursWorked <= 0)
                ModelState.AddModelError("HoursWorked", "Hours must be greater than 0.");

            // If any validation failed (not LecturerId anymore)
            if (!ModelState.IsValid)
                return View(model);

            model.DateSubmitted = DateTime.Now;
            model.Status = ClaimStatus.Pending;

            // -------------------- FILE UPLOAD --------------------
            if (upload != null && upload.Length > 0)
            {
                var allowed = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".xls" };
                var ext = Path.GetExtension(upload.FileName).ToLower();

                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("upload", "Invalid file type.");
                    return View(model);
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                string fileName = Guid.NewGuid() + ext;
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var fs = new FileStream(filePath, FileMode.Create))
                    await upload.CopyToAsync(fs);

                model.DocumentPath = "/uploads/" + fileName;
            }

            // -------------------- SAVE CLAIM --------------------
            _db.Claims.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Claim submitted successfully!";
            return RedirectToAction("Index");
        }

        // ------------------------- DETAILS -------------------------
        public async Task<IActionResult> Details(int id)
        {
            var claim = await _db.Claims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (!User.IsInRole("HR") && claim.LecturerId != user.Id)
                return Forbid();

            return View(claim);
        }
    }
}
