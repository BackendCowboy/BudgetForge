using BudgetForge.Application.DTOs;
using BudgetForge.Domain.Entities; // TransactionType
using FluentValidation;

namespace BudgetForge.Application.Validators
{
    public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
    {
        public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Amount)
            .Must(a => !a.HasValue || Math.Abs(a.Value) > 0m)
            .WithMessage("Amount must be non-zero.")
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .When(x => x.Description != null);

        RuleFor(x => x.Timestamp)
            .Must(ts => ts == null || ts <= DateTime.UtcNow.AddMinutes(2))
            .WithMessage(_ => $"'Timestamp' must be less than or equal to '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC'.")
            .When(x => x.Timestamp.HasValue);
    }
}
}