using Logistic_Shipment_tracker.DTOs;
using Logistic_Shipment_tracker.Models;
using System.ComponentModel.DataAnnotations;

namespace Logistic_Shipment_tracker.DTOs
{
    // Report DTOs
    public class GenerateReportRequest
    {
        [Required]
        public ReportType ReportType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }

    public class ReportResponse
    {
        public Guid Id { get; set; }
        public ReportType ReportType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? FilePath { get; set; }
        public DateTime GeneratedAt { get; set; }
        public UserResponse GeneratedBy { get; set; } = null!;
    }

    // Analytics DTOs
    public class DashboardAnalytics
    {
        public int TotalShipments { get; set; }
        public int ActiveShipments { get; set; }
        public int DeliveredShipments { get; set; }
        public int PendingShipments { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalCustomers { get; set; }
        public List<ShipmentStatusCount> ShipmentsByStatus { get; set; } = new();
        public List<MonthlyShipmentStats> MonthlyStats { get; set; } = new();
    }

    public class ShipmentStatusCount
    {
        public ShipmentStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyShipmentStats
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // Notification DTOs
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public NotificationType Type { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationStatus Status { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
