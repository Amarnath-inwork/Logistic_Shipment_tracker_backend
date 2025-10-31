
using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Logistic_Shipment_tracker.Services
{
    public interface IReportService
    {
        Task<string> GenerateShipmentReportAsync(
            DateTime startDate,
            DateTime endDate,
            ReportType reportType,
            Guid userId
        );
        Task<byte[]> GenerateShipmentReportPdfAsync(DateTime startDate, DateTime endDate);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReportService(ApplicationDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static DateTime EnsureUtc(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _ => dateTime,
            };
        }

        public async Task<string> GenerateShipmentReportAsync(
            DateTime startDate,
            DateTime endDate,
            ReportType reportType,
            Guid userId
        )
        {
            // Ensure dates are in UTC for PostgreSQL compatibility
            var utcStartDate = EnsureUtc(startDate);
            var utcEndDate = EnsureUtc(endDate);

            var shipments = await _context
                .Shipments.Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Where(s => s.CreatedAt >= utcStartDate && s.CreatedAt <= utcEndDate)
                .ToListAsync();

            var pdfBytes = await GenerateShipmentReportPdfAsync(utcStartDate, utcEndDate);

            // Save report to file
            var fileName = $"shipment_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var reportsDirectory = Path.Combine(_environment.ContentRootPath, "Reports");
            Directory.CreateDirectory(reportsDirectory);
            var filePath = Path.Combine(reportsDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            // Save report record to database - ensure UTC dates
            var report = new Report
            {
                GeneratedBy = userId,
                ReportType = reportType,
                StartDate = EnsureUtc(utcStartDate.Date),
                EndDate = EnsureUtc(utcEndDate.Date),
                FilePath = filePath,
                GeneratedAt = DateTime.UtcNow,
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return filePath;
        }

        public async Task<byte[]> GenerateShipmentReportPdfAsync(
            DateTime startDate,
            DateTime endDate
        )
        {
            // Ensure dates are in UTC for PostgreSQL compatibility
            var utcStartDate = EnsureUtc(startDate);
            var utcEndDate = EnsureUtc(endDate);

            var shipments = await _context
                .Shipments.Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Where(s => s.CreatedAt >= utcStartDate && s.CreatedAt <= utcEndDate)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text($"Shipment Report ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})")
                        .SemiBold()
                        .FontSize(20)
                        .FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            // Summary section
                            x.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Total Shipments").SemiBold();
                                            col.Item().Text(shipments.Count.ToString());
                                        });

                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Delivered").SemiBold();
                                            col.Item()
                                                .Text(
                                                    shipments
                                                        .Count(s =>
                                                            s.Status == ShipmentStatus.Delivered
                                                        )
                                                        .ToString()
                                                );
                                        });

                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("In Transit").SemiBold();
                                            col.Item()
                                                .Text(
                                                    shipments
                                                        .Count(s =>
                                                            s.Status == ShipmentStatus.InTransit
                                                        )
                                                        .ToString()
                                                );
                                        });

                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Pending").SemiBold();
                                            col.Item()
                                                .Text(
                                                    shipments
                                                        .Count(s =>
                                                            s.Status == ShipmentStatus.Created
                                                        )
                                                        .ToString()
                                                );
                                        });
                                });

                            // Shipments table
                            x.Item().Text("Shipment Details").SemiBold().FontSize(16);

                            x.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(100);
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(80);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Tracking #");
                                        header.Cell().Element(CellStyle).Text("Sender");
                                        header.Cell().Element(CellStyle).Text("Receiver");
                                        header.Cell().Element(CellStyle).Text("Destination");
                                        header.Cell().Element(CellStyle).Text("Status");

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container
                                                .DefaultTextStyle(x => x.SemiBold())
                                                .PaddingVertical(5)
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Black);
                                        }
                                    });

                                    foreach (var shipment in shipments)
                                    {
                                        table
                                            .Cell()
                                            .Element(CellStyle)
                                            .Text(shipment.TrackingNumber);
                                        table
                                            .Cell()
                                            .Element(CellStyle)
                                            .Text(shipment.Sender.FullName);
                                        table.Cell().Element(CellStyle).Text(shipment.ReceiverName);
                                        table
                                            .Cell()
                                            .Element(CellStyle)
                                            .Text(shipment.DestinationAddress);
                                        table
                                            .Cell()
                                            .Element(CellStyle)
                                            .Text(shipment.Status.ToString());

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .PaddingVertical(5);
                                        }
                                    }
                                });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
