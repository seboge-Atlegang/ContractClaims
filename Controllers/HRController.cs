using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ContractClaims.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;

        public HRController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _um = um;
        }

        public async Task<IActionResult> Dashboard()
        {
            var users = await _um.Users.ToListAsync();
            var totalUsers = users.Count;
            var claimsThisMonth = await _db.Claims.CountAsync(c => c.DateSubmitted >= DateTime.UtcNow.AddMonths(-1));
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ClaimsThisMonth = claimsThisMonth;
            return View(users);
        }

        public IActionResult Reports() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateApprovedClaimsPdf(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.UtcNow.AddMonths(-1);
            var t = to ?? DateTime.UtcNow;
            var claims = await _db.Claims.Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved && c.DateSubmitted >= f && c.DateSubmitted <= t)
                .ToListAsync();

            var bytes = CreatePdfBytes(claims, f, t);
            return File(bytes, "application/pdf", $"ApprovedClaims_{f:yyyyMMdd}_{t:yyyyMMdd}.pdf");
        }

        private byte[] CreatePdfBytes(List<Claim> claims, DateTime from, DateTime to)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(20);
                    page.Header()
                        .Text($"Approved Claims Report ({from:yyyy-MM-dd} → {to:yyyy-MM-dd})")
                        .FontSize(16).Bold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("ID").Bold();
                            header.Cell().Text("Lecturer").Bold();
                            header.Cell().Text("Hours").Bold();
                            header.Cell().Text("Rate").Bold();
                            header.Cell().Text("Amount").Bold();
                        });

                        foreach (var c in claims)
                        {
                            table.Cell().Text(c.Id.ToString());
                            table.Cell().Text($"{c.Lecturer?.FullName} ({c.Lecturer?.Email})");
                            table.Cell().Text(c.HoursWorked.ToString("N2"));
                            table.Cell().Text(c.HourlyRate.ToString("C"));
                            table.Cell().Text((c.HoursWorked * c.HourlyRate).ToString("C"));
                        }
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
