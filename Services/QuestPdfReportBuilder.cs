using ContractClaims.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace ContractClaims.Services
{
    public class QuestPdfReportBuilder
    {
        public byte[] BuildApprovedClaimsReport(List<Claim> claims, DateTime from, DateTime to, string logoLocalPath = null)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var total = claims.Sum(c => c.HoursWorked * c.HourlyRate);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text("Contract Monthly Claim System").FontSize(16).Bold().FontColor(Color.FromHex("#C71585"));
                            col.Item().Text("Approved Claims Report").FontSize(12).SemiBold();
                            col.Item().Text($"{from:yyyy-MM-dd} → {to:yyyy-MM-dd}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantColumn(80).AlignRight().Stack(s =>
                        {
                            if (!string.IsNullOrEmpty(logoLocalPath) && System.IO.File.Exists(logoLocalPath))
                            {
                                var imgBytes = System.IO.File.ReadAllBytes(logoLocalPath);
                                s.Item().Image(imgBytes);
                            }
                            else
                            {
                                s.Item().Text("CMCS").Bold().FontSize(14).FontColor(Color.FromHex("#C71585"));
                            }
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(40);
                                cd.RelativeColumn();
                                cd.ConstantColumn(80);
                                cd.ConstantColumn(80);
                                cd.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("ID").SemiBold();
                                header.Cell().Element(CellStyle).Text("Lecturer").SemiBold();
                                header.Cell().Element(CellStyle).Text("Hours").SemiBold();
                                header.Cell().Element(CellStyle).Text("Rate").SemiBold();
                                header.Cell().Element(CellStyle).Text("Amount").SemiBold();
                            });

                            foreach (var c in claims)
                            {
                                table.Cell().Element(CellStyle).Text(c.Id.ToString());
                                table.Cell().Element(CellStyle).Text($"{c.Lecturer?.FullName} ({c.Lecturer?.Email})");
                                table.Cell().Element(CellStyle).Text(c.HoursWorked.ToString("N2", CultureInfo.InvariantCulture));
                                table.Cell().Element(CellStyle).Text(c.HourlyRate.ToString("C", CultureInfo.InvariantCulture));
                                table.Cell().Element(CellStyle).AlignRight().Text((c.HoursWorked * c.HourlyRate).ToString("C", CultureInfo.InvariantCulture));
                            }

                            IContainer CellStyle(IContainer container)
                            {
                                return container.PaddingVertical(6).PaddingHorizontal(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                            }
                        });

                        col.Item().PaddingTop(10).AlignRight().Row(r =>
                        {
                            r.RelativeColumn();
                            r.ConstantColumn(250).Stack(s =>
                            {
                                s.Item().Text($"Total Approved Claims: {claims.Count}").Bold();
                                s.Item().Text($"Total Amount: {total.ToString("C", CultureInfo.InvariantCulture)}").FontSize(12).SemiBold();
                            });
                        });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated: ").SemiBold();
                            text.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm}");
                        });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
