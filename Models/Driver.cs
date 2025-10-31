using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Logistic_Shipment_tracker.Models
{

    public enum DriverStatus
    {
        Available,
        Busy,
        OffDuty,
        OnBreak
    }

    public class Driver
    {
        [Key]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [StringLength(255)]
        public string? CurrentAddress { get; set; }

        public DriverStatus Status { get; set; } = DriverStatus.Available;

        public DateTime? LastLocationUpdate { get; set; }

        public int MaxActiveShipments { get; set; } = 10;

        [Range(0, 5)]
        public decimal Rating { get; set; } = 0;

        public int CompletedShipments { get; set; } = 0;

        public int TotalRatings { get; set; } = 0;

        [StringLength(50)]
        public string? VehicleType { get; set; }

        [StringLength(50)]
        public string? LicenseNumber { get; set; }


        public bool IsVerified { get; set; } = false;

        public DateTime? LastActiveTime { get; set; }

        public TimeOnly? WorkStartTime { get; set; }
        public TimeOnly? WorkEndTime { get; set; }

        [StringLength(50)]
        public string? PreferredRegion { get; set; }

    }
}
