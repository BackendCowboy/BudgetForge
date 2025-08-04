using Microsoft.AspNetCore.Mvc;
using BudgetForge.Domain.Entities;

namespace BudgetForge.Api.Controllers
{
    /// <summary>
    /// API Controller for managing users
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        // For now, we'll use a simple list to store users (in-memory)
        // Later, we'll replace this with a real database
        private static readonly List<User> _users = new List<User>();
        private static int _nextId = 1;

        /// <summary>
        /// Get all users
        /// GET /api/users
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<User>> GetAllUsers()
        {
            return Ok(_users);
        }

        /// <summary>
        /// Get a specific user by ID
        /// GET /api/users/5
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<User> GetUser(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            return Ok(user);
        }

        /// <summary>
        /// Create a new user
        /// POST /api/users
        /// </summary>
        [HttpPost]
        public ActionResult<User> CreateUser(CreateUserRequest request)
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                return BadRequest("First name is required");
            }

            if (string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest("Last name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required");
            }

            // Check if email already exists
            if (_users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("A user with this email already exists");
            }

            // Create the new user
            var user = new User(request.FirstName, request.LastName, request.Email)
            {
                Id = _nextId++
            };

            _users.Add(user);

            // Return 201 Created with the new user
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        /// <summary>
        /// Update an existing user
        /// PUT /api/users/5
        /// </summary>
        [HttpPut("{id}")]
        public ActionResult<User> UpdateUser(int id, UpdateUserRequest request)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            // Update the user properties
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Check if new email already exists (excluding current user)
                if (_users.Any(u => u.Id != id && u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("A user with this email already exists");
                }
                user.Email = request.Email;
            }

            user.UpdatedAt = DateTime.UtcNow;

            return Ok(user);
        }

        /// <summary>
        /// Delete a user (soft delete)
        /// DELETE /api/users/5
        /// </summary>
        [HttpDelete("{id}")]
        public ActionResult DeleteUser(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            // Soft delete - just deactivate the user
            user.Deactivate();

            return NoContent(); // 204 No Content
        }

        /// <summary>
        /// Update user's last login time
        /// POST /api/users/5/login
        /// </summary>
        [HttpPost("{id}/login")]
        public ActionResult RecordLogin(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            if (!user.IsActive)
            {
                return BadRequest("User account is inactive");
            }

            user.UpdateLastLogin();

            return Ok(new { message = "Login recorded successfully", lastLogin = user.LastLoginAt });
        }
    }

    /// <summary>
    /// Request model for creating a new user
    /// </summary>
    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for updating an existing user
    /// </summary>
    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}