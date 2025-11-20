using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Web.Controllers
{
    [Authorize(Roles = "Coordinator,Manager")]
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoordinatorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Coordinator/Index
        public async Task<IActionResult> Index()
        {
            var claims = await _context.LecturerClaims
                .Include(c => c.Lecturer)
                .ToListAsync();

            return View(claims);
        }

        // POST: Coordinator/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.LecturerClaims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Approved;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _context.LecturerClaims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Rejected;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

    }
}
