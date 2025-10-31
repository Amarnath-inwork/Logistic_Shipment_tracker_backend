using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.DTOs;
using Logistic_Shipment_tracker.Models;
using Logistic_Shipment_tracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using System.Security.Claims;
using Microsoft.Extensions.Logging;



namespace Logistic_Shipment_tracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentsController : ControllerBase
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly IDriverAssignmentService _driverAssignmentService;
        private readonly ILogger<ShipmentsController> _logger;

        public ShipmentsController(
   ApplicationDBContext context,
       INotificationService notificationService,
      IDriverAssignmentService driverAssignmentService,
     ILogger<ShipmentsController> logger)
        {
            _dbContext = context;
            _notificationService = notificationService;
            _driverAssignmentService = driverAssignmentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShipmentResponse>>> GetShipments()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var UserId))
            {
                return BadRequest("Invalid User Id");

            }
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Shipment> query = _dbContext.Shipments
                .Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Include(s => s.TrackingUpdates)
                .ThenInclude(tu => tu.UpdatedByUser);

            // Filter based on user role
            query = currentUserRole switch
            {
                "Admin" => query,
                "Driver" => query.Where(s => s.AssignedDriverId == UserId),
                "Customer" => query.Where(s => s.SenderId == UserId),
                _ => query.Where(s => false),
            };

            // ✅ Sort by CreatedAt (newest first)
            query = query.OrderByDescending(s => s.CreatedAt);

            var shipments = await query
                .Select(s => new ShipmentResponse
                {
                    Id = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    Sender = new UserResponse
                    {
                        Id = s.Sender.Id,
                        FullName = s.Sender.FullName,
                        Email = s.Sender.Email,
                        Phone = s.Sender.Phone,
                        Role = s.Sender.Role,
                        CreatedAt = s.Sender.CreatedAt,
                    },
                    ReceiverName = s.ReceiverName,
                    ReceiverEmail = s.ReceiverEmail,
                    ReceiverPhone = s.ReceiverPhone,
                    OriginAddress = s.OriginAddress,
                    DestinationAddress = s.DestinationAddress,
                    OriginCity = s.OriginCity,
                    OriginRegion = s.OriginRegion,
                    DestinationCity = s.DestinationCity,
                    DestinationRegion = s.DestinationRegion,
                    Status = s.Status,
                    AssignedDriver =
                        s.AssignedDriver != null
                            ? new UserResponse
                            {
                                Id = s.AssignedDriver.Id,
                                FullName = s.AssignedDriver.FullName,
                                Email = s.AssignedDriver.Email,
                                Phone = s.AssignedDriver.Phone,
                                Role = s.AssignedDriver.Role,
                                CreatedAt = s.AssignedDriver.CreatedAt,
                            }
                            : null,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    TrackingUpdates = s
                        .TrackingUpdates.Select(tu => new TrackingUpdateResponse
                        {
                            Id = tu.Id,
                            Status = tu.Status,
                            Location = tu.Location,
                            Remarks = tu.Remarks,
                            UpdatedBy = new UserResponse
                            {
                                Id = tu.UpdatedByUser.Id,
                                FullName = tu.UpdatedByUser.FullName,
                                Email = tu.UpdatedByUser.Email,
                                Phone = tu.UpdatedByUser.Phone,
                                Role = tu.UpdatedByUser.Role,
                                CreatedAt = tu.UpdatedByUser.CreatedAt,
                            },
                            Timestamp = tu.Timestamp,
                        })
                        .ToList(),
                })
                .ToListAsync();
            return Ok(shipments);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ShipmentResponse>> GetShipment(Guid id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var UserId))
            {
                return BadRequest("Invalid User id");
            }

            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var shipment = await _dbContext
                .Shipments.Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Include(s => s.TrackingUpdates)
                .ThenInclude(tu => tu.UpdatedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (shipment == null)
            {
                return NotFound();
            }

            var hasAccess = currentUserRole switch
            {
                "Admin" => true,
                "Driver" => shipment.AssignedDriverId == UserId,
                "Customer" => shipment.SenderId == UserId,
                _ => false,
            };

            if (!hasAccess)
            {
                return Forbid();
            }
            var response = new ShipmentResponse
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                Sender = new UserResponse
                {
                    Id = shipment.Sender.Id,
                    FullName = shipment.Sender.FullName,
                    Email = shipment.Sender.Email,
                    Phone = shipment.Sender.Phone,
                    Role = shipment.Sender.Role,
                    CreatedAt = shipment.Sender.CreatedAt,
                },
                ReceiverName = shipment.ReceiverName,
                ReceiverEmail = shipment.ReceiverEmail,
                ReceiverPhone = shipment.ReceiverPhone,
                OriginAddress = shipment.OriginAddress,
                DestinationAddress = shipment.DestinationAddress,
                OriginCity = shipment.OriginCity,
                OriginRegion = shipment.OriginRegion,
                DestinationCity = shipment.DestinationCity,
                DestinationRegion = shipment.DestinationRegion,
                Status = shipment.Status,
                AssignedDriver =
                    shipment.AssignedDriver != null
                        ? new UserResponse
                        {
                            Id = shipment.AssignedDriver.Id,
                            FullName = shipment.AssignedDriver.FullName,
                            Email = shipment.AssignedDriver.Email,
                            Phone = shipment.AssignedDriver.Phone,
                            Role = shipment.AssignedDriver.Role,
                            CreatedAt = shipment.AssignedDriver.CreatedAt,
                        }
                        : null,
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt,
                TrackingUpdates = shipment
                    .TrackingUpdates.Select(tu => new TrackingUpdateResponse
                    {
                        Id = tu.Id,
                        Status = tu.Status,
                        Location = tu.Location,
                        Remarks = tu.Remarks,
                        UpdatedBy = new UserResponse
                        {
                            Id = tu.UpdatedByUser.Id,
                            FullName = tu.UpdatedByUser.FullName,
                            Email = tu.UpdatedByUser.Email,
                            Phone = tu.UpdatedByUser.Phone,
                            Role = tu.UpdatedByUser.Role,
                            CreatedAt = tu.UpdatedByUser.CreatedAt,
                        },
                        Timestamp = tu.Timestamp,
                    })
                    .ToList(),
            };

            return Ok(response);

        }



        [HttpPost]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<ActionResult<ShipmentResponse>> CreateShipment(CreateShipmentRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest("Invalid User Id");
            }

            // Create a temporary shipment object for driver availability check
            var tempShipment = new Shipment
            {
                Id = Guid.NewGuid(),
                TrackingNumber = GenerateTrackingNumber(),
                SenderId = userId,
                ReceiverName = request.ReceiverName,
                ReceiverEmail = request.ReceiverEmail,
                ReceiverPhone = request.ReceiverPhone,
                OriginAddress = request.OriginAddress,
                DestinationAddress = request.DestinationAddress,
                OriginCity = request.OriginCity,
                OriginRegion = request.OriginRegion,
                DestinationCity = request.DestinationCity,
                DestinationRegion = request.DestinationRegion,
            };

            // Add temporary shipment to context to allow driver assignment service to query it
            await _dbContext.Shipments.AddAsync(tempShipment);
            await _dbContext.SaveChangesAsync();

            // Check for available drivers before finalizing shipment creation
            var bestDriver = await _driverAssignmentService.GetBestDriverForShipmentAsync(
            tempShipment.Id,
                  request.Priority
            );

            // If no driver is available, rollback the shipment creation
            if (bestDriver == null)
            {
                _logger.LogWarning(
                     "No available drivers found for shipment with priority {Priority}. Shipment creation cancelled.",
                      request.Priority
                       );

                // Remove the temporary shipment
                _dbContext.Shipments.Remove(tempShipment);
                await _dbContext.SaveChangesAsync();

                return Conflict(new
                {
                    message = "No available drivers found for this shipment",
                    priority = request.Priority.ToString(),
                    reason = "All drivers are either busy, outside the service range, offline, or at maximum capacity"
                });
            }

            // Driver found - proceed with assignment
            var assignmentSuccess = await _driverAssignmentService.AssignDriverToShipmentAsync(
                    tempShipment.Id,
                    bestDriver.Driver.Id
            );

            if (!assignmentSuccess)
            {
                _logger.LogWarning(
                        "Failed to assign driver {DriverId} to shipment {ShipmentId}. Shipment creation cancelled.",
                             bestDriver.Driver.Id,
                           tempShipment.Id
                      );

                // Remove the shipment if assignment failed
                _dbContext.Shipments.Remove(tempShipment);
                await _dbContext.SaveChangesAsync();

                return Conflict(new
                {
                    message = "Failed to assign driver to shipment",
                    reason = "Driver became unavailable during assignment process"
                });
            }

            _logger.LogInformation(
                "Auto-assigned driver {DriverId} to shipment {ShipmentId} with priority {Priority}",
                    bestDriver.Driver.Id,
                    tempShipment.Id,
                request.Priority
                    );

            // Send driver assignment notifications to driver, sender, and receiver
            await _notificationService.NotifyDriverAssignmentAsync(tempShipment.Id, bestDriver.Driver.Id);

            // Reload shipment with all related data
            var shipment = await _dbContext
         .Shipments.Include(s => s.Sender)
          .Include(s => s.AssignedDriver)
                 .FirstAsync(s => s.Id == tempShipment.Id);

            // Notify about shipment creation
            await _notificationService.NotifyShipmentStatusChangeAsync(shipment, "Created");

            var response = new ShipmentResponse
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                Sender = new UserResponse
                {
                    Id = shipment.Sender.Id,
                    FullName = shipment.Sender.FullName,
                    Email = shipment.Sender.Email,
                    Phone = shipment.Sender.Phone,
                    Role = shipment.Sender.Role,
                    CreatedAt = shipment.Sender.CreatedAt,
                },
                ReceiverName = shipment.ReceiverName,
                ReceiverEmail = shipment.ReceiverEmail,
                ReceiverPhone = shipment.ReceiverPhone,
                OriginAddress = shipment.OriginAddress,
                DestinationAddress = shipment.DestinationAddress,
                OriginCity = shipment.OriginCity,
                OriginRegion = shipment.OriginRegion,
                DestinationCity = shipment.DestinationCity,
                DestinationRegion = shipment.DestinationRegion,
                Status = shipment.Status,
                AssignedDriver = shipment.AssignedDriver != null
           ? new UserResponse
           {
               Id = shipment.AssignedDriver.Id,
               FullName = shipment.AssignedDriver.FullName,
               Email = shipment.AssignedDriver.Email,
               Phone = shipment.AssignedDriver.Phone,
               Role = shipment.AssignedDriver.Role,
               CreatedAt = shipment.AssignedDriver.CreatedAt,
           }
           : null,
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt,
                TrackingUpdates = new List<TrackingUpdateResponse>()
            };

            return CreatedAtAction(nameof(GetShipment), new { id = shipment.Id }, response);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> UpdateShipment(Guid id, UpdateShipmentStatusRequest request)
        {
            var stringUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(stringUserId) || !Guid.TryParse(stringUserId, out var currentUserId))
            {
                return BadRequest("Invalid User Id");
            }

            var shipment = await _dbContext.Shipments
                .Include(u => u.AssignedDriver)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
            {
                return NotFound();
            }

            if (shipment.AssignedDriverId != currentUserId)
            {
                return Forbid();
            }
            // Prevent updates once shipment is delivered or cancelled
            if (shipment.Status == ShipmentStatus.Delivered)
            {
                return BadRequest("Cannot update a shipment that has already been delivered");
            }

            if (shipment.Status == ShipmentStatus.Cancelled)
            {
                return BadRequest("Cannot update a shipment that has been cancelled");
            }

            shipment.Status = request.Status;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Create tracking update
            var trackingUpdate = new TrackingUpdate
            {
                ShipmentId = id,
                Status = request.Status.ToString(),
                Location = request.Location,
                Remarks = request.Remarks,
                UpdatedBy = currentUserId,
            };

            _dbContext.TrackingUpdates.Add(trackingUpdate);

            // Handle delivery completion logic
            if (request.Status == ShipmentStatus.Delivered)
            {
                await HandleShipmentDeliveryAsync(currentUserId);
            }

            // Handle cancellation logic
            if (request.Status == ShipmentStatus.Cancelled)
            {
                await HandleShipmentCancellationAsync(currentUserId);
            }

            await _dbContext.SaveChangesAsync();

            // send notification 
            await _notificationService.NotifyShipmentStatusChangeAsync(
                shipment,
                request.Status.ToString()
            );

            return NoContent();
        }


        [HttpGet("{id}/rating-status")]
        public async Task<ActionResult<ShipmentRatingStatusResponse>> CheckShipmentRatingStatus(Guid id)
        {
            var stringUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? currentUserId = null;

            if (!string.IsNullOrEmpty(stringUserId) && Guid.TryParse(stringUserId, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }


            var ratingStatus = await _driverAssignmentService.CheckShipmentRatingStatusAsync(id, currentUserId);
            if (ratingStatus == null)
            {
                return NotFound("Shipment not found");
            }
            return Ok(ratingStatus);
        }


        [HttpGet("track/{trackingNumber}")]
        [AllowAnonymous]
        public async Task<ActionResult<ShipmentResponse>> TrackShipment(string trackingNumber)
        {
            var shipment = await _dbContext.Shipments
                .Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Include(s => s.TrackingUpdates)
                .ThenInclude(tu => tu.UpdatedByUser)
                .FirstOrDefaultAsync(u => u.TrackingNumber == trackingNumber);

            if (shipment == null)
            {
                return NotFound("Shipment not found");
            }

            var response = new ShipmentResponse
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                Sender = new UserResponse
                {
                    Id = shipment.Sender.Id,
                    FullName = shipment.Sender.FullName,
                    Email = shipment.Sender.Email,
                    Phone = shipment.Sender.Phone,
                    Role = shipment.Sender.Role,
                    CreatedAt = shipment.Sender.CreatedAt,
                },
                ReceiverName = shipment.ReceiverName,
                ReceiverEmail = shipment.ReceiverEmail,
                ReceiverPhone = shipment.ReceiverPhone,
                OriginAddress = shipment.OriginAddress,
                DestinationAddress = shipment.DestinationAddress,
                OriginCity = shipment.OriginCity,
                OriginRegion = shipment.OriginRegion,
                DestinationCity = shipment.DestinationCity,
                DestinationRegion = shipment.DestinationRegion,
                Status = shipment.Status,
                AssignedDriver =
                    shipment.AssignedDriver != null
                        ? new UserResponse
                        {
                            Id = shipment.AssignedDriver.Id,
                            FullName = shipment.AssignedDriver.FullName,
                            Email = shipment.AssignedDriver.Email,
                            Phone = shipment.AssignedDriver.Phone,
                            Role = shipment.AssignedDriver.Role,
                            CreatedAt = shipment.AssignedDriver.CreatedAt,
                        }
                        : null,
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt,
                TrackingUpdates = shipment
                    .TrackingUpdates.Select(tu => new TrackingUpdateResponse
                    {
                        Id = tu.Id,
                        Status = tu.Status,
                        Location = tu.Location,
                        Remarks = tu.Remarks,
                        UpdatedBy = new UserResponse
                        {
                            Id = tu.UpdatedByUser.Id,
                            FullName = tu.UpdatedByUser.FullName,
                            Email = tu.UpdatedByUser.Email,
                            Phone = tu.UpdatedByUser.Phone,
                            Role = tu.UpdatedByUser.Role,
                            CreatedAt = tu.UpdatedByUser.CreatedAt,
                        },
                        Timestamp = tu.Timestamp,
                    })
                    .OrderBy(tu => tu.Timestamp)
                    .ToList(),
            };

            return Ok(response);

        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> CancelShipment(Guid id, CancelShipmentRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (
                string.IsNullOrEmpty(userIdString)
                || !Guid.TryParse(userIdString, out var currentUserId)
            )
            {
                return BadRequest("Invalid user ID");
            }
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var shipment = await _dbContext
                .Shipments.Include(s => s.AssignedDriver)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
            {
                return NotFound();
            }

            // Check permissions - customers can only cancel their own shipments
            if (currentUserRole == "Customer" && shipment.SenderId != currentUserId)
            {
                return Forbid();
            }

            // Prevent cancelling already delivered or cancelled shipments
            if (shipment.Status == ShipmentStatus.Delivered)
            {
                return BadRequest("Cannot cancel a shipment that has already been delivered");
            }

            if (shipment.Status == ShipmentStatus.Cancelled)
            {
                return BadRequest("Shipment is already cancelled");
            }

            shipment.Status = ShipmentStatus.Cancelled;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Create tracking update
            var trackingUpdate = new TrackingUpdate
            {
                ShipmentId = id,
                Status = ShipmentStatus.Cancelled.ToString(),
                Location = request.Location ?? "System",
                Remarks = request.Reason ?? "Shipment cancelled",
                UpdatedBy = currentUserId,
            };

            _dbContext.TrackingUpdates.Add(trackingUpdate);

            // Handle cancellation logic if shipment was assigned to a driver
            if (shipment.AssignedDriverId.HasValue)
            {
                await HandleShipmentCancellationAsync(shipment.AssignedDriverId.Value);
            }

            await _dbContext.SaveChangesAsync();

            // Send notification
            await _notificationService.NotifyShipmentStatusChangeAsync(shipment, "Cancelled");

            return NoContent();
        }

        private async Task HandleShipmentDeliveryAsync(Guid driverId)
        {
            var driver = await _dbContext.Drivers.FindAsync(driverId);
            if (driver == null) return;

            driver.CompletedShipments++;
            driver.LastActiveTime = DateTime.UtcNow;

            var activeShipmentCount = await _dbContext.Shipments.Where(s =>
            s.AssignedDriverId == driverId
            && s.Status != ShipmentStatus.Delivered
            && s.Status != ShipmentStatus.Cancelled).CountAsync();

            if (activeShipmentCount == 0)
            {
                driver.Status = DriverStatus.Available;
            }
            else if (activeShipmentCount < driver.MaxActiveShipments)
            {
                if (driver.Status == DriverStatus.Busy)
                {
                    driver.Status = DriverStatus.Available;
                }
            }
        }

        private async Task HandleShipmentCancellationAsync(Guid driverId)
        {
            var driver = await _dbContext.Drivers.FindAsync(driverId);
            if (driver == null) return;
            driver.LastActiveTime = DateTime.UtcNow;

            // Check remaining active shipments for this driver
            var activeShipmentsCount = await _dbContext
                .Shipments.Where(s =>
                    s.AssignedDriverId == driverId
                    && s.Status != ShipmentStatus.Delivered
                    && s.Status != ShipmentStatus.Cancelled
                )
                .CountAsync();

            if (activeShipmentsCount == 0)
            {
                driver.Status = DriverStatus.Available;
            }
            else if (activeShipmentsCount < driver.MaxActiveShipments)
            {
                if (driver.Status == DriverStatus.Busy)
                {
                    driver.Status = DriverStatus.Available;
                }
            }

        }


        private static string GenerateTrackingNumber()
        {
            var random = new Random();
            var prefix = "LST";
            var number = random.Next(100000, 999999);
            return $"{prefix}{number}";
        }
    }
}
