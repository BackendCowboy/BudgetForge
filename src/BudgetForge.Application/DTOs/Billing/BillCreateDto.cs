using BudgetForge.Domain.Enums.Billing;

namespace BudgetForge.Application.DTOs.Billing;

public record BillCreateDto(
    string Name,
    decimal Amount,
    DateOnly DueDate,
    bool IsRecurring,
    Frequency? Frequency,
    string? Category,
    bool AutoPay
);