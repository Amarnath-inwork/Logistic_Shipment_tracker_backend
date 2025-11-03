using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.DTOs;
using Logistic_Shipment_tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Logistic_Shipment_tracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public AuditLogsController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: api/auditlogs
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AuditLogPagedResponse>> GetAuditLogs([FromQuery] AuditLogQueryRequest request)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            // Apply filters
            if (request.UserId.HasValue)
            {
                query = query.Where(a => a.UserId == request.UserId.Value);
            }

            if (!string.IsNullOrEmpty(request.Action))
            {
                query = query.Where(a => a.Action.Contains(request.Action));
            }

            if (!string.IsNullOrEmpty(request.TargetTable))
            {
                query = query.Where(a => a.TargetTable == request.TargetTable);
            }

            if (request.StartDate.HasValue)
            {
                var startDateUtc = DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
                query = query.Where(a => a.Timestamp >= startDateUtc);
            }

            if (request.EndDate.HasValue)
            {
                var endDateUtc = DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
                query = query.Where(a => a.Timestamp <= endDateUtc);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var logs = await query
                      .OrderByDescending(a => a.Timestamp)
                      .Skip((request.PageNumber - 1) * request.PageSize)
                      .Take(request.PageSize)
                        .Select(a => new AuditLogResponse
                        {
                            Id = a.Id,
                            UserId = a.UserId,
                            UserName = a.User.FullName,
                            Action = a.Action,
                            TargetTable = a.TargetTable,
                            TargetId = a.TargetId,
                            Timestamp = a.Timestamp
                        })
                     .ToListAsync();

            var response = new AuditLogPagedResponse
            {
                Logs = logs,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };

            return Ok(response);
        }

        // GET: api/auditlogs/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AuditLogResponse>> GetAuditLog(Guid id)
        {
            var auditLog = await _context.AuditLogs
           .Include(a => a.User)
           .FirstOrDefaultAsync(a => a.Id == id);

            if (auditLog == null)
            {
                return NotFound();
            }

            var response = new AuditLogResponse
            {
                Id = auditLog.Id,
                UserId = auditLog.UserId,
                UserName = auditLog.User.FullName,
                Action = auditLog.Action,
                TargetTable = auditLog.TargetTable,
                TargetId = auditLog.TargetId,
                Timestamp = auditLog.Timestamp
            };

            return Ok(response);
        }

        // GET: api/auditlogs/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<AuditLogResponse>>> GetUserAuditLogs(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var logs = await _context.AuditLogs
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new AuditLogResponse
                       {
                           Id = a.Id,
                           UserId = a.UserId,
                           UserName = a.User.FullName,
                           Action = a.Action,
                           TargetTable = a.TargetTable,
                           TargetId = a.TargetId,
                           Timestamp = a.Timestamp
                       })
                    .ToListAsync();

            return Ok(logs);
        }

        // GET: api/auditlogs/target/{targetTable}/{targetId}
        [HttpGet("target/{targetTable}/{targetId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<AuditLogResponse>>> GetTargetAuditLogs(string targetTable, Guid targetId)
        {
            var logs = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.TargetTable == targetTable && a.TargetId == targetId)
                .OrderByDescending(a => a.Timestamp)
                    .Select(a => new AuditLogResponse
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        UserName = a.User.FullName,
                        Action = a.Action,
                        TargetTable = a.TargetTable,
                        TargetId = a.TargetId,
                        Timestamp = a.Timestamp
                    })
                .ToListAsync();

                return Ok(logs);
            }

        // GET: api/auditlogs/my-activity
        [HttpGet("my-activity")]
        public async Task<ActionResult<List<AuditLogResponse>>> GetMyActivity([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            var logs = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.UserId == currentUserId)
                .OrderByDescending(a => a.Timestamp)
                    .Skip((pageNumber - 1) * pageSize)
                          .Take(pageSize)
                .Select(a => new AuditLogResponse
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.User.FullName,
                    Action = a.Action,
                    TargetTable = a.TargetTable,
                    TargetId = a.TargetId,
                    Timestamp = a.Timestamp
                })
               .ToListAsync();

            return Ok(logs);
        }
    }
}
