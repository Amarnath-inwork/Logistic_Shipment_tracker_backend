using Logistic_Shipment_tracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistic_Shipment_tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistanceController : ControllerBase
    {
        private readonly IDistanceService _distanceService;
        private readonly ILogger<DistanceController> _logger;

        public DistanceController(
            IDistanceService distanceService,
            ILogger<DistanceController> logger
        )
        {
            _distanceService = distanceService;
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint to calculate distance between two addresses
        /// </summary>
        [HttpGet("calculate")]
        [AllowAnonymous] // Allow testing without authentication
        public async Task<ActionResult<DistanceCalculationResponse>> CalculateDistance(
            [FromQuery] string origin,
            [FromQuery] string destination
        )
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            {
                return BadRequest("Both origin and destination addresses are required");
            }

            try
            {
                _logger.LogInformation(
                    "Calculating distance between '{Origin}' and '{Destination}'",
                    origin,
                    destination
                );

                var distance = await _distanceService.CalculateDistanceAsync(origin, destination);
                var originCoords = await _distanceService.GeocodeAddressAsync(origin);
                var destCoords = await _distanceService.GeocodeAddressAsync(destination);

                var response = new DistanceCalculationResponse
                {
                    Origin = origin,
                    Destination = destination,
                    DistanceKm = distance,
                    OriginCoordinates = new CoordinateResponse
                    {
                        Latitude = originCoords.latitude,
                        Longitude = originCoords.longitude,
                    },
                    DestinationCoordinates = new CoordinateResponse
                    {
                        Latitude = destCoords.latitude,
                        Longitude = destCoords.longitude,
                    },
                    CalculatedAt = DateTime.UtcNow,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error calculating distance between '{Origin}' and '{Destination}'",
                    origin,
                    destination
                );
                return StatusCode(500, "Error calculating distance");
            }
        }

        /// <summary>
        /// Test endpoint to geocode a single address
        /// </summary>
        [HttpGet("geocode")]
        [AllowAnonymous] // Allow testing without authentication
        public async Task<ActionResult<GeocodeResponse>> GeocodeAddress([FromQuery] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("Address is required");
            }

            try
            {
                _logger.LogInformation("Geocoding address '{Address}'", address);

                var (latitude, longitude) = await _distanceService.GeocodeAddressAsync(address);

                if (latitude == 0 && longitude == 0)
                {
                    return NotFound($"Could not geocode address: {address}");
                }

                var response = new GeocodeResponse
                {
                    Address = address,
                    Coordinates = new CoordinateResponse
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                    },
                    GeocodedAt = DateTime.UtcNow,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address '{Address}'", address);
                return StatusCode(500, "Error geocoding address");
            }
        }

        /// <summary>
        /// Get cache statistics for the distance service
        /// </summary>
        [HttpGet("cache-stats")]
        [Authorize(Roles = "Admin")]
        public ActionResult<CacheStatsResponse> GetCacheStats()
        {
            var (count, expired) = NominatimDistanceService.GetCacheStats();

            return Ok(
                new CacheStatsResponse
                {
                    TotalCachedAddresses = count,
                    ExpiredEntries = expired,
                    ActiveEntries = count - expired,
                    CheckedAt = DateTime.UtcNow,
                }
            );
        }

        /// <summary>
        /// Clear the geocoding cache (Admin only)
        /// </summary>
        [HttpPost("clear-cache")]
        [Authorize(Roles = "Admin")]
        public ActionResult ClearCache()
        {
            NominatimDistanceService.ClearCache();
            _logger.LogInformation("Geocoding cache cleared by admin");
            return NoContent();
        }
    }

    // DTOs for the distance controller
    public class DistanceCalculationResponse
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public CoordinateResponse OriginCoordinates { get; set; } = new();
        public CoordinateResponse DestinationCoordinates { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
    }

    public class GeocodeResponse
    {
        public string Address { get; set; } = string.Empty;
        public CoordinateResponse Coordinates { get; set; } = new();
        public DateTime GeocodedAt { get; set; }
    }

    public class CoordinateResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CacheStatsResponse
    {
        public int TotalCachedAddresses { get; set; }
        public int ExpiredEntries { get; set; }
        public int ActiveEntries { get; set; }
        public DateTime CheckedAt { get; set; }
    }
}
