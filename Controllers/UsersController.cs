using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.DTOs;
using Logistic_Shipment_tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Logistic_Shipment_tracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDBContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers([FromQuery] UserRole? role = null)
        {
            var query = _context.Users.AsQueryable();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            var users = await query.Select(u =>
                new UserResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                }).ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetUser(Guid id)
        {
            var stringUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(stringUserId) ||  !Guid.TryParse(stringUserId, out Guid userId))
            {
                return BadRequest("Invalid User Id");
            }

            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if(userId != id && currentUserRole != "Admin" )
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var response = new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id , UpdateUserRequest request)
        {
            var stringUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(stringUserId) || !Guid.TryParse(stringUserId, out Guid userId)) {
                return BadRequest("Invalid User id");
            }

            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (id != userId && currentUserRole != "Admin")
            {
                return Forbid();
            }
            var user = await _context.Users.FindAsync(id);

            if(user == null)
            {
                return NotFound();
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrEmpty(request.Email))
            {
                // Check if email is already taken by another user
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                {
                    return BadRequest("Email is already taken");
                }
                user.Email = request.Email;
            }

            if (request.Phone != null)
                user.Phone = request.Phone;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if(user == null)
            {
                return NotFound();
            }
            if (user.Role == UserRole.Admin)
            {
                return BadRequest("Can not delete Admin");
            }

            if (user.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers.FindAsync(id);
                if (driver != null)
                {
                    // Check if driver has any active shipments
                    var hasActiveShipments = await _context.Shipments
                        .AnyAsync(s => s.AssignedDriverId == id &&
                                      (s.Status == ShipmentStatus.Created ||
                                       s.Status == ShipmentStatus.InTransit ||
                                       s.Status == ShipmentStatus.PickedUp));

                    if (hasActiveShipments)
                    {
                        return BadRequest("Cannot delete driver with active shipments");
                    }

                    // Remove driver record first
                    _context.Drivers.Remove(driver);
                }
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return BadRequest("This driver is already assigned to an shipment");
            }
            
            return NoContent();
        }

        [HttpGet("drivers")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetDrivers()
        {
            var drivers = await _context.Users
                .Where(u => u.Role == UserRole.Driver).Select(u => new UserResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                }).ToListAsync();
            return Ok(drivers);
        }
    }
}
