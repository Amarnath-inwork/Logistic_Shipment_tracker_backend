using Logistic_Shipment_tracker.Models;
using System.ComponentModel.DataAnnotations;

namespace Logistic_Shipment_tracker.DTOs
{
    public class DriverRecommendationResponse
    {
        public UserResponse Driver { get; set; } = null!;
        public DriverDetailsResponse DriverDetails { get; set; } = null!;
        public double Distance { get; set; }
        public int ActiveShipments { get; set; }
        public decimal Rating { get; set; }
        public int CompletedShipments { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public double Score { get; set; }
        public string RecommendationReason { get; set; } = string.Empty;
        public List<string> RecommendationFactors { get; set; } = new();
    }

    public class DriverDetailsResponse
    {
        public DriverStatus Status { get; set; }
        public string? CurrentAddress { get; set; }
        public int MaxActiveShipments { get; set; }
        public string? VehicleType { get; set; }
        public string? LicenseNumber { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? LastActiveTime { get; set; }
        public TimeOnly? WorkStartTime { get; set; }
        public TimeOnly? WorkEndTime { get; set; }
        public string? PreferredRegion { get; set; }
    }

    public class UpdateDriverLocationRequest
    {
        [Required]
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateDriverStatusRequest
    {
        [Required]
        public DriverStatus Status { get; set; }
    }

    public class SmartAssignDriverRequest
    {
        public Guid? PreferredDriverId { get; set; }
        public bool UseAutoAssignment { get; set; } = true;
        public int MaxRecommendations { get; set; } = 5;
        public AssignmentPriority Priority { get; set; } = AssignmentPriority.Balanced;
    }

    public class DriverAvailabilityResponse
    {
        public UserResponse Driver { get; set; } = null!;
        public DriverDetailsResponse DriverDetails { get; set; } = null!;
        public int ActiveShipments { get; set; }
        public bool IsAvailable { get; set; }
        public string AvailabilityReason { get; set; } = string.Empty;

        // Rating details
        public decimal Rating { get; set; }
        public int CompletedShipments { get; set; }
        public int TotalRatings { get; set; }
        public DateTime? LastLocationUpdate { get; set; }

        // Performance metrics
        public string PerformanceCategory { get; set; } = string.Empty;
        public List<string> PerformanceFactors { get; set; } = new();
    }

    public class UpdateDriverProfileRequest
    {
        [Range(1, 5)]
        public int MaxActiveShipments { get; set; } = 5;

        [StringLength(50)]
        public string? VehicleType { get; set; }

        [StringLength(20)]
        public string? LicenseNumber { get; set; }

        public TimeOnly? WorkStartTime { get; set; }
        public TimeOnly? WorkEndTime { get; set; }

        [StringLength(50)]
        public string? PreferredRegion { get; set; }
    }

    public class RateDriverRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string? Comment { get; set; }

        [Required]
        public Guid ShipmentId { get; set; }
    }

    public class UpdateDriverVerificationRequest
    {
        [Required]
        public bool IsVerified { get; set; }

        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters")]
        public string? Reason { get; set; }
    }

    public class DriverRatingResponse
    {
        public Guid DriverId { get; set; }
        public UserResponse Driver { get; set; } = null!;
        public decimal AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int CompletedShipments { get; set; }
        public bool IsVerified { get; set; }
        public List<DriverRatingDetail> RecentRatings { get; set; } = new();
    }

    public class DriverRatingDetail
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime RatedAt { get; set; }
        public string RatedByCustomer { get; set; } = string.Empty;
        public string ShipmentTrackingNumber { get; set; } = string.Empty;
    }

    public class ShipmentRatingStatusResponse
    {
        public Guid ShipmentId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public bool IsRated { get; set; }
        public DriverRatingDetail? ExistingRating { get; set; }
        public UserResponse? Driver { get; set; }
        public bool CanBeRated { get; set; }
        public string? RatingIneligibilityReason { get; set; }
    }

    public enum AssignmentPriority
    {
        Distance, // Prioritize closest drivers
        Experience, // Prioritize experienced drivers
        Rating, // Prioritize highest-rated drivers
        Availability, // Prioritize drivers with least workload
        Balanced, // Balanced scoring across all factors
    }
}
