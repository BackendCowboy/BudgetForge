using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetForge.Domain.Entities;
using BudgetForge.Infrastructure.Data;

namespace BudgetForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(CreateUserRequest request)
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("First name is required");
            
            if (string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest("Last name is required");
            
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required");

            // Check for duplicate email
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("A user with this email already exists");
            }

            // Create user
            var user = new User(request.FirstName, request.LastName, request.Email);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(int id, UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            // Update properties
            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;
            
            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;
            
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Id != id && u.Email == request.Email))
                {
                    return BadRequest("A user with this email already exists");
                }
                user.Email = request.Email;
            }

            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            user.Deactivate();
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/login")]
        public async Task<ActionResult> RecordLogin(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            if (!user.IsActive)
            {
                return BadRequest("User account is inactive");
            }

            user.UpdateLastLogin();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Login recorded successfully", lastLogin = user.LastLoginAt });
        }
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}