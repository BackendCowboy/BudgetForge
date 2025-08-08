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
        public AccountType Type { get; set; } // Changed from string to AccountType enum

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Initial balance cannot be negative")]
        public decimal InitialBalance { get; set; }

        public string Currency { get; set; } = "CAD";
    }

    public class UpdateAccountRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public AccountType Type { get; set; } // Using AccountType enum
    }

    public class AccountResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; } // Changed from string to AccountType enum
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "CAD";
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } // Added UpdatedAt
    }

    // Transaction DTOs
    public class CreateTransactionRequest
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public TransactionType Type { get; set; } // Changed from string to TransactionType enum

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty; // Kept as Description (removed Category)

        public DateTime? Date { get; set; }
    }

    public class UpdateTransactionRequest
    {
        [Required]
        public TransactionType Type { get; set; } // Using TransactionType enum

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }

    public class TransactionResponse
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; } // Changed from string to TransactionType enum
        public string Description { get; set; } = string.Empty; // Changed from Category to Description
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } // Added UpdatedAt
    }
}