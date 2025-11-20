using ContractClaims.Data;
using ContractClaims.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractClaims.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly UserManager<ApplicationUser> _um;
        private readonly ApplicationDbContext _db;

        public HRController(UserManager<ApplicationUser> um, ApplicationDbContext db)
        {
            _um = um;
            _db = db;
        }

        public IActionResult Index()
        {
            var users = _um.Users.ToList();
            return View(users);
        }

        // Edit user, Create user actions (use _um.CreateAsync and AddToRoleAsync)
        // ...

        public IActionResult Reports()
        {
            // show UI to generate a report (date range, type)
            return View();
        }

        [HttpPost]
        public IActionResult GenerateReportAsPdf(DateTime? from, DateTime? to)
        {
            var qFrom = from ?? DateTime.UtcNow.AddMonths(-1);
            var qTo = to ?? DateTime.UtcNow;
            var claims = _db.LecturerClaims
                .Where(c => c.Status == ClaimStatus.Approved && c.DateSubmitted >= qFrom && c.DateSubmitted <= qTo)
                .ToList();

            var pdfBytes = BuildClaimsReportPdf(claims, qFrom, qTo);

            return File(pdfBytes, "application/pdf", $"ApprovedClaims_{qFrom:yyyyMMdd}_{qTo:yyyyMMdd}.pdf");
        }

        private byte[] BuildClaimsReportPdf(List<LecturerClaim> claims, DateTime from, DateTime to)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.Content()
                        .Column(col =>
                        {
                            col.Item().Text($"Approved Claims Report").FontSize(20).Bold();
                            col.Item().Text($"From {from:yyyy-MM-dd} To {to:yyyy-MM-dd}");
                            col.Item().Table(table =>
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
                                    header.Cell().Element(CellStyle).Text("ID");
                                    header.Cell().Element(CellStyle).Text("Lecturer");
                                    header.Cell().Element(CellStyle).Text("Hours");
                                    header.Cell().Element(CellStyle).Text("Rate");
                                    header.Cell().Element(CellStyle).Text("Total");
                                });

                                foreach (var c in claims)
                                {
                                    table.Cell().Element(CellStyle).Text(c.Id.ToString());
                                    table.Cell().Element(CellStyle).Text($"{c.Lecturer?.FirstName} {c.Lecturer?.LastName} ({c.Lecturer?.Email})");
                                    table.Cell().Element(CellStyle).Text(c.HoursWorked.ToString("N2"));
                                    table.Cell().Element(CellStyle).Text(c.HourlyRate.ToString("C"));
                                    table.Cell().Element(CellStyle).Text((c.HoursWorked * c.HourlyRate).ToString("C"));
                                }

                                static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(3);
                            });
                        });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
