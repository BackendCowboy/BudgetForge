using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetForge.Infrastructure.Data;
using BudgetForge.Application.Services;
using BudgetForge.Application.DTOs;
using BudgetForge.Domain.Entities;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BudgetForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(ApplicationDbContext context, IJwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return Conflict(new { message = "User with this email already exists" });
            }

            // Create new user with hashed password
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate tokens
            var roles = new List<string> { "User" };
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15); // Adjust based on your JWT settings
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Store refresh token in database (you'll need to add a RefreshTokens table)
            // For now, we'll just return it

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Roles = roles,
                    LastLoginAt = DateTime.UtcNow
                }
            });
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate tokens
            var roles = new List<string> { "User" };
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15); // Adjust based on your JWT settings
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Update last login
            user.UpdateLastLogin();
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Roles = roles,
                    LastLoginAt = user.LastLoginAt
                }
            });
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate the refresh token (you should store and validate refresh tokens in database)
            // For now, this is a simplified version
            
            // Extract user info from the expired access token
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid access token" });
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token claims" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "User not found or inactive" });
            }

            // Generate new tokens
            var roles = new List<string> { "User" };
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            return Ok(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Roles = roles,
                    LastLoginAt = user.LastLoginAt
                }
            });
        }

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Verify current password
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Update password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>
        /// Logout (client should discard tokens)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // In a real implementation, you might want to:
            // 1. Invalidate the refresh token in the database
            // 2. Add the access token to a blacklist (if using token blacklisting)
            
            return Ok(new { message = "Logged out successfully" });
        }

        #region Helper Methods

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Add salt for better security
                var salt = GenerateSalt();
                var combinedPassword = password + salt;
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedPassword));
                
                // Store salt with hash (first 16 bytes are salt, rest is hash)
                var result = new byte[salt.Length + hashedBytes.Length];
                Array.Copy(salt, 0, result, 0, salt.Length);
                Array.Copy(hashedBytes, 0, result, salt.Length, hashedBytes.Length);
                
                return Convert.ToBase64String(result);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            
            // Extract salt (first 16 bytes)
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            
            // Hash the provided password with the extracted salt
            using (var sha256 = SHA256.Create())
            {
                var combinedPassword = password + Convert.ToBase64String(salt);
                var computedHash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedPassword));
                
                // Compare the computed hash with stored hash (skip salt bytes)
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (hashBytes[i + 16] != computedHash[i])
                        return false;
                }
            }
            
            return true;
        }

        private byte[] GenerateSalt()
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        #endregion
    }
}