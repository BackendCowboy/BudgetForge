using BudgetForge.Application.DTOs;
using FluentValidation;

namespace BudgetForge.Application.Validators
{
    public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
    {
        public CreateAccountRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0m);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
        }
    }

    public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
    {
        public UpdateAccountRequestValidator()
        {
            RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name != null);
            RuleFor(x => x.Currency).Length(3).When(x => x.Currency != null);
        }
    }
}