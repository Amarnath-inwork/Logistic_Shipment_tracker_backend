using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Logistic_Shipment_tracker.Models
{

    public enum ReportType
    {
        Daily,
        Weekly,
        Monthly,
        Custom
    }

    public class Report
    {
        public Guid Id { get; set; }

        [Required]
        public Guid GeneratedBy { get; set; }

        [Required]
        public ReportType ReportType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(255)]
        public string? FilePath { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // navigation properties

        [ForeignKey("GeneratedBy")]
        public User GeneratedByUser { get; set; } = null!;
    }
}
