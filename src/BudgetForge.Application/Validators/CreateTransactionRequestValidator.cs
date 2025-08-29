using BudgetForge.Application.DTOs;
using BudgetForge.Domain.Entities; // TransactionType
using FluentValidation;

namespace BudgetForge.Application.Validators
{
    public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0);

            // Allow any sign, but require non-zero absolute value
            RuleFor(x => x.Amount)
                .Must(a => Math.Abs(a) > 0m)
                .WithMessage("Amount must be non-zero.");

            RuleFor(x => x.Type)
                .IsInEnum();

            RuleFor(x => x.Description)
                .MaximumLength(200);

            RuleFor(x => x.Timestamp)
                .Must(ts => ts == null || ts <= DateTime.UtcNow.AddMinutes(2))
                .WithMessage(_ => $"'Timestamp' must be less than or equal to '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC'.");
        }
    }
}