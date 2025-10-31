using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Logistic_Shipment_tracker.Models
{
    public class DriverRating
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DriverId { get; set; }
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public Guid ShipmentId { get; set; }

        [Required]
        [Range(0, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties

        [ForeignKey("DriverId")]
        public User Driver { get; set; } = null!;

        [ForeignKey("CustomerId")]
        public User Customer { get; set; } = null!;

        [ForeignKey("ShipmentId")]
        public Shipment Shipment { get; set; } = null!;

    }
}
