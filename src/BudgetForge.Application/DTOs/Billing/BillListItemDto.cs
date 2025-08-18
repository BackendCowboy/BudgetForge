using BudgetForge.Domain.Enums.Billing;

namespace BudgetForge.Application.DTOs.Billing;

public record BillListItemDto(
    Guid Id,
    string Name,
    decimal Amount,
    DateOnly DueDate,
    bool IsRecurring,
    Frequency? Frequency,
    string? Category,
    bool AutoPay,
    int DaysUntilDue,
    BillStatus Status
);