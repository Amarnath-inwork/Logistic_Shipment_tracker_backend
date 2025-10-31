using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Logistic_Shipment_tracker.Models
{

    public enum NotificationType
    {
        Email ,
        SMS
    }

    public enum NotificationStatus
    {
        Sent ,
        Failed,
        Pending
    }


    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ShipmentId { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [StringLength(100)]
        public string Recipient { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        public DateTime? SentAt { get; set; }

        // navigation pproperties

        [ForeignKey("ShipmentId")]
        public Shipment Shipment { get; set; } = null!;


    }
}
