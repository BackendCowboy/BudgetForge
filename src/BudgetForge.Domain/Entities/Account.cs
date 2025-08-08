using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetForge.Domain.Entities
{
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public AccountType Type { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "CAD";

        // This is auto-calculated from transactions, not user-editable directly
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }
}