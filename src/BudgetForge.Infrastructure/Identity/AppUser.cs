using Microsoft.AspNetCore.Identity;
using BudgetForge.Domain.Entities;

namespace BudgetForge.Infrastructure.Identity
{
    public class AppUser : IdentityUser<int>
    {
        // Profile fields you used before
        public string FirstName { get; set; } = string.Empty;
        public string LastName  { get; set; } = string.Empty;
        public bool IsActive    { get; set; } = true;

        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt  { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        // It’s OK to reference Domain entities here (Infrastructure depends on Domain)
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}