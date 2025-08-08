using Microsoft.AspNetCore.Identity;

namespace BudgetForge.Domain.Entities
{
    public class AppRole : IdentityRole<int>
    {
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}