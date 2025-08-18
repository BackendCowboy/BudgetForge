namespace BudgetForge.Application.DTOs.Billing;

public record BillPaymentCreateDto(
    decimal Amount,
    DateTime? PaidAt,
    string? Notes
);