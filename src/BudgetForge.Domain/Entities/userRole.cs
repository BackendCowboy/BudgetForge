using System;

namespace BudgetForge.Domain.Entities
{
    /// <summary>
    /// Junction table for many-to-many relationship between Users and Roles
    /// This allows a user to have multiple roles, and a role to be assigned to multiple users
    /// </summary>
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedBy { get; set; } = "System"; // Could be user ID or system
        
        // Navigation properties - these create the relationships
        public virtual User User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;

        public UserRole()
        {
            AssignedAt = DateTime.UtcNow;
        }

        public UserRole(int userId, int roleId) : this()
        {
            UserId = userId;
            RoleId = roleId;
        }
    }
}