using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.DTOs;
using Logistic_Shipment_tracker.Models;
using Logistic_Shipment_tracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Logistic_Shipment_tracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IReportService _reportService;

        public ReportsController(ApplicationDBContext context , IReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        [HttpPost("generate")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult<ReportResponse>> GenerateReport(
            GenerateReportRequest request
            )
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (
                string.IsNullOrEmpty(userIdString)
                || !Guid.TryParse(userIdString, out var currentUserId)
            )
            {
                return BadRequest("Invalid user ID");
            }

            var filePath = await _reportService.GenerateShipmentReportAsync(
                request.StartDate,
                request.EndDate,
                request.ReportType,
                currentUserId
                );

            var report = await _context
                .Reports.Include(r => r.GeneratedByUser)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstAsync(r => r.GeneratedBy == currentUserId);

            var response = new ReportResponse
            {
                Id = report.Id,
                ReportType = report.ReportType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                FilePath = report.FilePath,
                GeneratedAt = report.GeneratedAt,
                GeneratedBy = new UserResponse
                {
                    Id = report.GeneratedByUser.Id,
                    FullName = report.GeneratedByUser.FullName,
                    Email = report.GeneratedByUser.Email,
                    Phone = report.GeneratedByUser.Phone,
                    Role = report.GeneratedByUser.Role,
                    CreatedAt = report.GeneratedByUser.CreatedAt,
                },
            };

            return Ok(response);
        }

        [HttpGet("{id}/download")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> DownloadReport(Guid id)
        {
            var report = await _context.Reports.FindAsync(id);
            if(report == null || report.FilePath == null || !System.IO.File.Exists(report.FilePath)){
                return NotFound();
            }
            var fileBytes = await System.IO.File.ReadAllBytesAsync(report.FilePath);
            var fileName = Path.GetFileName(report.FilePath);

            return File(fileBytes, "application/pdf", fileName);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ReportResponse>>> GetReports()
        {
            var reports = await _context
                .Reports.Include(r => r.GeneratedByUser)
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new ReportResponse
                {
                    Id = r.Id,
                    ReportType = r.ReportType,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    FilePath = r.FilePath,
                    GeneratedAt = r.GeneratedAt,
                    GeneratedBy = new UserResponse
                    {
                        Id = r.GeneratedByUser.Id,
                        FullName = r.GeneratedByUser.FullName,
                        Email = r.GeneratedByUser.Email,
                        Phone = r.GeneratedByUser.Phone,
                        Role = r.GeneratedByUser.Role,
                        CreatedAt = r.GeneratedByUser.CreatedAt,
                    },
                })
                .ToListAsync();

            return Ok(reports);
        }

        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DashboardAnalytics>> GetAnalytics()
        {
            var totalShipments = await _context.Shipments.CountAsync();
            var activeShipments = await _context.Shipments.CountAsync(s =>
                s.Status == ShipmentStatus.PickedUp || s.Status == ShipmentStatus.InTransit
            );
            var deliveredShipments = await _context.Shipments.CountAsync(s =>
                s.Status == ShipmentStatus.Delivered
            );
            var pendingShipments = await _context.Shipments.CountAsync(s =>
                s.Status == ShipmentStatus.Created
            );
            var totalDrivers = await _context.Users.CountAsync(u => u.Role == UserRole.Driver);
            var totalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer);

            var shipmentsByStatus = await _context
                .Shipments.GroupBy(s => s.Status)
                .Select(g => new ShipmentStatusCount { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Get monthly data with separate year/month properties to avoid EF translation issues
            var monthlyData = await _context
                .Shipments.Where(s => s.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            // Format the month strings on the client side
            var monthlyStats = monthlyData
                .Select(m => new MonthlyShipmentStats
                {
                    Month = $"{m.Year}-{m.Month:D2}",
                    Count = m.Count,
                })
                .ToList();

            var analytics = new DashboardAnalytics
            {
                TotalShipments = totalShipments,
                ActiveShipments = activeShipments,
                DeliveredShipments = deliveredShipments,
                PendingShipments = pendingShipments,
                TotalDrivers = totalDrivers,
                TotalCustomers = totalCustomers,
                ShipmentsByStatus = shipmentsByStatus,
                MonthlyStats = monthlyStats,
            };

            return Ok(analytics);
        }

    }
}
