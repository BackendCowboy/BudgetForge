using System.ComponentModel.DataAnnotations;
using BudgetForge.Domain.Entities;

namespace BudgetForge.Application.DTOs
{
    // Account DTOs
    public class CreateAccountRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public AccountType Type { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Initial balance cannot be negative")]
        public decimal InitialBalance { get; set; } = 0;

        public string Currency { get; set; } = "CAD";
    }

    // Use nullable members for partial update semantics
    public class UpdateAccountRequest
    {
        public string? Name { get; set; }
        public AccountType? Type { get; set; }
        public string? Currency { get; set; }
    }

    public class AccountResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "CAD";
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Transaction DTOs
    public class CreateTransactionRequest
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        [Required]
        public TransactionType Type { get; set; } // Income or Expense (optionally Transfer later)

        [MaxLength(200)]
        public string? Description { get; set; } // optional is nicer for UX

        public DateTime? Timestamp { get; set; } // defaulted in service to UtcNow
    }

    // Nullable for partial updates
    public class UpdateTransactionRequest
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal? Amount { get; set; }

        public TransactionType? Type { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        public DateTime? Timestamp { get; set; }
    }

    public class TransactionResponse
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsDeleted { get; set; }          // added
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}