using System.ComponentModel.DataAnnotations;

namespace Logistic_Shipment_tracker.Models
{

    public enum UserRole
    {
        Customer,
        Driver,
        Admin
    }

    public class User
    {
        private string _email = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public required string Email
        {
            get => _email;
            set => _email = value?.ToLowerInvariant() ?? string.Empty;
        }

        [Required]
        [StringLength(255)]
        public required string Password { get; set; } = string.Empty;

        [StringLength(100)]
        public  string? Phone { get; set; }

        [Required]
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Shipment> SentShipments { get; set; } = new List<Shipment>();
        public ICollection<Shipment> AssignedShipments { get; set; } = new List<Shipment>();
        public ICollection<TrackingUpdate> TrackingUpdates { get; set; } = new List<TrackingUpdate>();
        public ICollection<Report> GeneratedReports { get; set; } = new List<Report>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();


    }
}
