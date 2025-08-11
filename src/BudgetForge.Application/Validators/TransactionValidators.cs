using BudgetForge.Application.DTOs;
using FluentValidation;

namespace BudgetForge.Application.Validators
{
    public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0);
            RuleFor(x => x.Amount).GreaterThan(0m);
            RuleFor(x => x.Description).MaximumLength(200);
            // Optional: future-proof timestamp
            RuleFor(x => x.Timestamp)
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .When(x => x.Timestamp.HasValue);
        }
    }

    public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
    {
        public UpdateTransactionRequestValidator()
        {
            RuleFor(x => x.Amount).GreaterThan(0m).When(x => x.Amount.HasValue);
            RuleFor(x => x.Description).MaximumLength(200).When(x => x.Description != null);
            RuleFor(x => x.Timestamp)
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .When(x => x.Timestamp.HasValue);
        }
    }
}