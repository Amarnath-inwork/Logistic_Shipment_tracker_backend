using Logistic_Shipment_tracker.DTOs;
using Logistic_Shipment_tracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Logistic_Shipment_tracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriversController : ControllerBase
    {
        private readonly IDriverAssignmentService _driverAssignmentService;
        public DriversController(IDriverAssignmentService driverAssignmentService)
        {
            _driverAssignmentService = driverAssignmentService;
        }

        [HttpPost("location")]
        [Authorize(Roles ="Driver")]
        public async Task<IActionResult> UpdateLocation(UpdateDriverLocationRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            await _driverAssignmentService.UpdateDriverLocationAsync(
                currentUserId,
                request.Address
                );

            return NoContent();
        }

        [HttpPut("status")]
        [Authorize(Roles ="Driver")]
        public async Task<IActionResult> UpdateStatus(UpdateDriverStatusRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (
                string.IsNullOrEmpty(userIdString)
                || !Guid.TryParse(userIdString, out var currentUserId)
            )
            {
                return BadRequest("Invalid user ID");
            }
            await _driverAssignmentService.UpdateDriverStatusAsync(currentUserId, request.Status);
            return NoContent();
        }

        [HttpPost("profile")]
        [Authorize(Roles ="Driver")]
        public async Task<IActionResult> CreateProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (
                string.IsNullOrEmpty(userIdString)
                || !Guid.TryParse(userIdString, out var currentUserId)
            )
            {
                return BadRequest("Invalid user ID");
            }

            var success = await _driverAssignmentService.CreateDriverProfileAsync(currentUserId);
            if(!success)
            {
                return BadRequest("Driver profile already exists or user is not a driver");
            }
            return NoContent();
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> UpdateProfile(UpdateDriverProfileRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (
                string.IsNullOrEmpty(userIdString)
                || !Guid.TryParse(userIdString, out var currentUserId)
            )
            {
                return BadRequest("Invalid user ID");
            }

            var success = await _driverAssignmentService.UpdateDriverProfileAsync(
                currentUserId,
                request
            );
            if (!success)
            {
                return NotFound("Driver profile not found");
            }

            return NoContent();
        }


        /// <summary>
        /// Rate a driver after completed shipment
        /// </summary>
        [HttpPost("{driverId}/rate")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> RateDriver(
            Guid driverId,
            [FromBody] RateDriverRequest request
        )
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (
                string.IsNullOrEmpty(userIdString)
                || !Guid.TryParse(userIdString, out var currentUserId)
            )
            {
                return BadRequest("Invalid user ID");
            }

            var success = await _driverAssignmentService.RateDriverAsync(
                driverId,
                currentUserId,
                request
            );
            if (!success)
            {
                return BadRequest(
                    "Unable to rate driver. Driver not found or you haven't completed a shipment with this driver."
                );
            }

            return NoContent();
        }

        /// <summary>
        /// Verify or unverify a driver (Admin only)
        /// </summary>
        [HttpPut("{driverId}/verification")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDriverVerification(
            Guid driverId,
            [FromBody] UpdateDriverVerificationRequest request
        )
        {
            var success = await _driverAssignmentService.UpdateDriverVerificationAsync(
                driverId,
                request.IsVerified
            );
            if (!success)
            {
                return NotFound("Driver not found");
            }

            return NoContent();
        }

        /// <summary>
        /// Get driver rating history
        /// </summary>
        [HttpGet("{driverId}/ratings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DriverRatingResponse>> GetDriverRating(Guid driverId)
        {
            var rating = await _driverAssignmentService.GetDriverRatingAsync(driverId);
            if (rating == null)
            {
                return NotFound("Driver not found");
            }

            return Ok(rating);
        }


    }
}
