using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BudgetForge.Infrastructure.Identity; // <-- gives you AppUser
using BudgetForge.Infrastructure.Data;
using BudgetForge.Application.Services;
using BudgetForge.Application.DTOs;
using BudgetForge.Domain.Entities;

namespace BudgetForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // NEW: Identity services
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        // Keep your JWT service for token issuance
        private readonly IJwtTokenService _jwtTokenService;

        // Optional: DbContext only if you want direct queries (not required for most Identity ops)
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IJwtTokenService jwtTokenService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _context = context;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Identity: check if user exists by email
            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return Conflict(new { message = "User with this email already exists" });

            // Create new Identity user (password rules & hashing handled by Identity)
            var user = new AppUser
            {
                Email = request.Email,
                UserName = request.Email,              // Identity requires a username; we mirror email
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = false                 // you can enable email confirmation later
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                // Return Identity validation errors in a simple shape
                var errors = createResult.Errors.Select(e => $"{e.Code}: {e.Description}");
                return BadRequest(new { message = "Registration failed", errors });
            }

            // Ensure the default "User" role exists + assign it
            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15); // keep aligned with your JwtSettings
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // If you want to persist refresh tokens, do it here (you have a RefreshTokens table)
            // _context.RefreshTokens.Add(new RefreshToken(refreshToken, user.Id, refreshTokenExpiry, HttpContext.Connection.RemoteIpAddress?.ToString()));
            // await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Roles = roles,
                    LastLoginAt = null
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
                return BadRequest(ModelState);

            // Identity lookup
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "Invalid email or password" });

            // Identity password check (+ optional lockout-on-failure)
            var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!signIn.Succeeded)
                return Unauthorized(new { message = "Invalid email or password" });

            // Update last login (keep your domain semantics)
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Optional: persist refresh token
            // _context.RefreshTokens.Add(new RefreshToken(refreshToken, user.Id, refreshTokenExpiry, HttpContext.Connection.RemoteIpAddress?.ToString()));
            // await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email!,
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
                return BadRequest(ModelState);

            // Validate the refresh token against your store if you persist them (recommended).
            // For now, this follows your simplified version.

            // Extract user info from the (possibly expired) access token
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return Unauthorized(new { message = "Invalid access token" });

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Invalid token claims" });

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "User not found or inactive" });

            // Issue new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, roles);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Optional: rotate & persist refresh token here

            return Ok(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email!,
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
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Identity handles old/new password validation + hashing
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => $"{e.Code}: {e.Description}");
                return BadRequest(new { message = "Password change failed", errors });
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>
        /// Logout (client should discard tokens)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // With JWT, logout is client-side unless you're persisting/blacklisting refresh tokens.
            // If you persist refresh tokens, revoke here.
            return Ok(new { message = "Logged out successfully" });
        }
    }
}