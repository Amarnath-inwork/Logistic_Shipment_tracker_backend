using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Logistic_Shipment_tracker.Models
{

    public enum ShipmentStatus
    {
        Created,
        PickedUp,
        InTransit,
        Delivered,
        Cancelled
    }

    public class Shipment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)]
        public string TrackingNumber { get; set; } = string.Empty;

        [Required]

        public  Guid SenderId { get; set; } 

        [Required]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string ReceiverEmail { get; set; } = string.Empty;

        [StringLength(15)]
        public string? ReceiverPhone { get; set; }

        [Required]
        public string OriginAddress { get; set; } = string.Empty;

        [Required]
        public string DestinationAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? OriginCity { get; set; }


        [StringLength(100)]
        public string? OriginRegion { get; set; }

        [StringLength(100)]
        public string? DestinationCity {  get; set; }

        [StringLength(100)]
        public string? DestinationRegion { get; set; }


        [Required]
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
        
        public Guid? AssignedDriverId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // foreignKeys

        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        [ForeignKey("AssignedDriverId")]
        public User? AssignedDriver { get; set; }

        public ICollection<TrackingUpdate> TrackingUpdates { get; set; } = new List<TrackingUpdate>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
