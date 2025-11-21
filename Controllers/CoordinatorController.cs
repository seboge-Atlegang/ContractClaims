using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractClaims.Controllers
{
    [Authorize(Roles = "Coordinator,Manager,HR")]
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;

        public CoordinatorController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _um = um;
        }

        // Pending claims view
        public async Task<IActionResult> Index()
        {
            ViewBag.Role = (await _um.GetUserAsync(User)) is ApplicationUser me && User.IsInRole("Coordinator") ? "Coordinator" : "Manager/HR";
            var list = await _db.Claims
                .Include(c => c.Lecturer)
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null) return NotFound();
            claim.Status = ClaimStatus.Approved;
            claim.ReviewedAt = DateTime.UtcNow;
            claim.ReviewedById = _um.GetUserId(User);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Claim #{id} approved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason = null)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null) return NotFound();
            claim.Status = ClaimStatus.Rejected;
            claim.ReviewedAt = DateTime.UtcNow;
            claim.ReviewedById = _um.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(reason))
            {
                claim.Notes = (claim.Notes ?? "") + $"\n\nRejection reason: {reason}";
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Claim #{id} rejected.";
            return RedirectToAction(nameof(Index));
        }
    }
}
