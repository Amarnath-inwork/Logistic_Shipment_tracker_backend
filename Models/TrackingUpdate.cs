using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Logistic_Shipment_tracker.Models
{
    public class TrackingUpdate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ShipmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Location { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public Guid? UpdatedBy { get; set; }


        // Navigation Properties

        [ForeignKey("ShipmentId")]
        public Shipment Shipment { get; set; } = null!;


        [ForeignKey("UpdatedBy")]
        public User? UpdatedByUser { get; set; }
    }
}
