using ContractClaims.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace ContractClaims.Services
{
    public class QuestPdfReportBuilder
    {
        public byte[] BuildApprovedClaimsReport(List<Claim> claims, DateTime from, DateTime to, string logoPath = null)
        {
            // Required for QuestPDF on Windows
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var culture = new CultureInfo("en-US"); // SAFE currency culture
            var total = claims.Sum(c => (c.HoursWorked * c.HourlyRate));

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    // ================= HEADER =================
                    page.Header().Row(row =>
                    {
                        // Left side text block
                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text("Contract Monthly Claim System")
                                .FontSize(16).Bold().FontColor(Color.FromHex("#C71585"));

                            col.Item().Text("Approved Claims Report")
                                .FontSize(12).SemiBold();

                            col.Item().Text($"{from:yyyy-MM-dd} → {to:yyyy-MM-dd}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        // Right side logo
                        row.ConstantColumn(70).AlignRight().Stack(s =>
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(logoPath) && System.IO.File.Exists(logoPath))
                                {
                                    var bytes = System.IO.File.ReadAllBytes(logoPath);
                                    s.Item().Image(bytes);
                                }
                                else
                                {
                                    s.Item().Text("CMCS")
                                        .Bold().FontSize(14)
                                        .FontColor(Color.FromHex("#C71585"));
                                }
                            }
                            catch
                            {
                                s.Item().Text("CMCS").Bold().FontSize(14);
                            }
                        });
                    });

                    // ================= CONTENT =================
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            // Table header
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);  // ID
                                c.RelativeColumn();    // Lecturer
                                c.ConstantColumn(60);  // Hours
                                c.ConstantColumn(60);  // Rate
                                c.ConstantColumn(80);  // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(Cell).Text("ID").SemiBold();
                                header.Cell().Element(Cell).Text("Lecturer").SemiBold();
                                header.Cell().Element(Cell).Text("Hours").SemiBold();
                                header.Cell().Element(Cell).Text("Rate").SemiBold();
                                header.Cell().Element(Cell).Text("Amount").SemiBold();
                            });

                            // Table rows
                            foreach (var c in claims)
                            {
                                var lecturerName = c.Lecturer?.FullName ?? "Unknown Lecturer";
                                var lecturerEmail = c.Lecturer?.Email ?? "Unknown Email";

                                table.Cell().Element(Cell).Text(c.Id.ToString());
                                table.Cell().Element(Cell).Text($"{lecturerName} ({lecturerEmail})");
                                table.Cell().Element(Cell).Text(c.HoursWorked.ToString("N2", culture));
                                table.Cell().Element(Cell).Text(c.HourlyRate.ToString("C", culture));
                                table.Cell().Element(Cell).AlignRight().Text((c.HoursWorked * c.HourlyRate).ToString("C", culture));
                            }

                            static IContainer Cell(IContainer c)
                            {
                                return c.PaddingVertical(6)
                                        .PaddingHorizontal(6)
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten3);
                            }
                        });

                        // Totals section
                        col.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Total Approved Claims: {claims.Count}").Bold();
                            c.Item().Text($"Total Amount: {total.ToString("C", culture)}")
                             .FontSize(12).SemiBold();
                        });
                    });

                    // ================= FOOTER =================
                    page.Footer()
                        .AlignCenter()
                        .Text(t =>
                        {
                            t.Span("Generated: ").SemiBold();
                            t.Span($"{DateTime.Now:yyyy-MM-dd HH:mm}");
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
