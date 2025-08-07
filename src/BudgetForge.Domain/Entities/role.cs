using System;
using System.Collections.Generic;

namespace BudgetForge.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public Role()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Role(string name, string description) : this()
        {
            Name = name;
            Description = description;
        }
    }
}