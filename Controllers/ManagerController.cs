using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContractClaims.Controllers
{
    [Authorize(Roles = "Manager,Coordinator,HR")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _userManager = um;
        }

        public IActionResult Index()
        {
            var pending = _db.Claims.Where(c => c.Status == ClaimStatus.Pending).OrderBy(c => c.DateSubmitted).ToList();
            return View(pending);
        }

        [HttpPost]
        public async Task<IActionResult> Review(int id, string actionType)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (actionType == "approve")
                claim.Status = ClaimStatus.Approved;
            else if (actionType == "reject")
                claim.Status = ClaimStatus.Rejected;

            claim.ReviewedById = user.Id;
            claim.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
