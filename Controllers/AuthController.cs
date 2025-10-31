using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.DTOs;
using Logistic_Shipment_tracker.Models;
using Logistic_Shipment_tracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Logistic_Shipment_tracker.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IWebHostEnvironment _environment;
        private readonly IDriverAssignmentService _driverAssignmentService;
        private readonly IAuditLogService _auditLogService;

        public AuthController(ApplicationDBContext context , ITokenService tokenService , IWebHostEnvironment environment , IDriverAssignmentService driverAssignmentService, IAuditLogService auditLogService)
        {
            _dbContext = context;
            _tokenService = tokenService;
            _environment = environment;
            _driverAssignmentService = driverAssignmentService;
            _auditLogService = auditLogService;
        }

        private CookieOptions GetCookieOptions()
        {
            var isProduction = _environment.IsProduction();
            return new CookieOptions
            {
                HttpOnly = true,
                // For cross-origin requests (React frontend), we need SameSite=None and Secure=true
                Secure = true, // Must be true for SameSite=None
                SameSite = SameSiteMode.None, // Required for cross-origin cookies in both dev and prod
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/", // Explicit path
                Domain = null, // Let browser handle domain automatically for cross-origin
            };
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            if(await _dbContext.Users.AnyAsync(u => u.Email == request.Email)){
                return BadRequest("User with this Email already exists");
            }


            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone = request.Phone,
                Role = request.Role,
            };

            
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            if (request.Role == UserRole.Driver)
            {
                await _driverAssignmentService.CreateDriverProfileAsync(user.Id);
            }

            // Log user registration
            await _auditLogService.LogActionAsync(user.Id, "User registered", "Users", user.Id);

            var token = _tokenService.GenerateToken(user);
            Response.Cookies.Append("auth_token", token, GetCookieOptions());
            var response = new AuthResponse
            {
                User = new UserResponse
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                }
            };
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("Invalid Credentials");
            }

            if(!BCrypt.Net.BCrypt.Verify(request.Password , user.Password))
            {
                return BadRequest("Invalid Credentials");
            }

            // Log user login
            await _auditLogService.LogActionAsync(user.Id, "User logged in", "Users", user.Id);

            var token = _tokenService.GenerateToken(user);

            Response.Cookies.Append("auth_token" , token , GetCookieOptions());
            var response = new AuthResponse
            {
                User = new UserResponse
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                }
            };
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> logout()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                // Log user logout
                await _auditLogService.LogActionAsync(userId, "User logged out", "Users", userId);
            }

            Response.Cookies.Delete("auth_token", GetCookieOptions());
            return Ok(new { message = "Logged out successfully"});
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserResponse>> GetCurrentUser()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId)){
                return BadRequest("Invalid User Id");
            }
             var user = await _dbContext.Users.FindAsync(userId);
            if(user == null)
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
    }
}
