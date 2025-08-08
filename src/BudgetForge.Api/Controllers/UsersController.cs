using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using BudgetForge.Infrastructure.Identity; // AppUser

namespace BudgetForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // lock the controller to admins by default
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /api/users
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToList();

            return Ok(users);
        }

        // GET: /api/users/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound($"User with ID {id} not found");

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToArray()
            });
        }

        // POST: /api/users
        // Creates an Identity user WITHOUT setting a password (admin-initiated account)
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("First name is required");
            if (string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest("Last name is required");
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required");

            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null) return BadRequest("A user with this email already exists");

            var user = new AppUser
            {
                Email = request.Email,
                UserName = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            IdentityResult result;
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                // Create with password
                result = await _userManager.CreateAsync(user, request.Password);
            }
            else
            {
                // Create without password — user must set/reset later
                result = await _userManager.CreateAsync(user);
            }

            if (!result.Succeeded) return BadRequest(result.Errors);

            // Default role "User" if requested
            if (request.AssignDefaultUserRole)
            {
                const string defaultRole = "User";
                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    var createRole = await _roleManager.CreateAsync(new IdentityRole<int>(defaultRole));
                    if (!createRole.Succeeded) return BadRequest(createRole.Errors);
                }
                var addRole = await _userManager.AddToRoleAsync(user, defaultRole);
                if (!addRole.Succeeded) return BadRequest(addRole.Errors);
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id, user.Email });
        }

        // PUT: /api/users/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound($"User with ID {id} not found");

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existing = await _userManager.FindByEmailAsync(request.Email);
                if (existing != null && existing.Id != id)
                    return BadRequest("A user with this email already exists");
                user.Email = request.Email;
                user.UserName = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { user.Id, user.Email, user.FirstName, user.LastName });
        }

        // DELETE: /api/users/5  (soft delete by deactivating)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound($"User with ID {id} not found");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return NoContent();
        }

        // POST: /api/users/5/login  (record a login timestamp)
        [HttpPost("{id:int}/login")]
        public async Task<IActionResult> RecordLogin(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound($"User with ID {id} not found");
            if (!user.IsActive) return BadRequest("User account is inactive");

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { message = "Login recorded successfully", lastLogin = user.LastLoginAt });
        }

        // POST: /api/users/5/roles
        [HttpPost("{id:int}/roles")]
        public async Task<IActionResult> AddRoles(int id, [FromBody] UpdateRolesRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            foreach (var role in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var create = await _roleManager.CreateAsync(new IdentityRole<int>(role));
                    if (!create.Succeeded) return BadRequest(create.Errors);
                }
            }

            var result = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return NoContent();
        }

        // DELETE: /api/users/5/roles/Admin
        [HttpDelete("{id:int}/roles/{role}")]
        public async Task<IActionResult> RemoveRole(int id, string role)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            var result = await _userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return NoContent();
        }

        // GET: /api/users/me (any authenticated user)
        [HttpGet("me")]
        [Authorize] // anyone signed-in
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                Roles = roles
            });
        }
    }

    // DTOs for this controller
    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UserDetailDto : UserSummaryDto
    {
        public string[] Roles { get; set; } = Array.Empty<string>();
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public string Email     { get; set; } = "";
        public string? Password { get; set; } // optional; if omitted, user is created without password
        public bool AssignDefaultUserRole { get; set; } = true;
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }
        public string? Email     { get; set; }
    }

    public class UpdateRolesRequest
    {
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}