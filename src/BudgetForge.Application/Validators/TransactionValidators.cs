using BudgetForge.Application.DTOs;
using FluentValidation;

namespace BudgetForge.Application.Validators
{
    public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0);

            RuleFor(x => x.Amount)
                .GreaterThan(0m);

            RuleFor(x => x.Type)
                .IsInEnum();

            RuleFor(x => x.Description)
                .MaximumLength(200);

            // Evaluate "now" at validation time; allow small positive skew (2 minutes)
            RuleFor(x => x.Timestamp)
                .Must(ts => ts == null || ts <= DateTime.UtcNow.AddMinutes(2))
                .WithMessage(_ => $"'Timestamp' must be less than or equal to '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC'.");
        }
    }

    public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
    {
        public UpdateTransactionRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0m)
                .When(x => x.Amount.HasValue);

            RuleFor(x => x.Type)
                .IsInEnum()
                .When(x => x.Type.HasValue);

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .When(x => x.Description != null);

            // Same dynamic "now" logic with small skew
            RuleFor(x => x.Timestamp)
                .Must(ts => ts == null || ts <= DateTime.UtcNow.AddMinutes(2))
                .WithMessage(_ => $"'Timestamp' must be less than or equal to '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC'.")
                .When(x => x.Timestamp.HasValue);
        }
    }
}