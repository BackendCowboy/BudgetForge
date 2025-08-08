using System;
using System.Collections.Generic;

namespace BudgetForge.Domain.Entities
{
    /// <summary>
    /// Represents a user in the BudgetForge application
    /// </summary>
    public class User
    {
        // Primary key - every entity needs a unique identifier
        public int Id { get; set; }

        // User's personal information
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Authentication properties
        public string PasswordHash { get; set; } = string.Empty;

        // Timestamps - important for auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // User status
        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = false;

        // Navigation properties for authentication
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // Navigation properties for budgeting
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

     // Computed property - combines first and last name
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Constructor - runs when you create a new User
        public User()
        {
            // Set default values
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        // Constructor with parameters - for creating a user with initial data
        public User(string firstName, string lastName, string email) : this()
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
        }

        // Method to update the last login time
        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Method to deactivate the user (soft delete)
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        // Method to activate the user
        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        // Method to update user information
        public void UpdateInfo(string firstName, string lastName, string email)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            UpdatedAt = DateTime.UtcNow;
        }

        // Method to update password hash
        public void UpdatePasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
            UpdatedAt = DateTime.UtcNow;
        }

        // Override ToString for easy debugging
        public override string ToString()
        {
            return $"User: {FullName} ({Email}) - Active: {IsActive}";
        }
    }
}