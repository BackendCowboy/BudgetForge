using BudgetForge.Domain.Enums.Billing;

namespace BudgetForge.Application.DTOs.Billing;

public record BillPaymentItemDto(Guid Id, decimal Amount, DateTime PaidAt, string? Notes);

public record BillDetailsDto(
    Guid Id,
    string Name,
    decimal Amount,
    DateOnly DueDate,
    bool IsRecurring,
    Frequency? Frequency,
    string? Category,
    bool AutoPay,
    bool IsActive,
    DateTime? LastPaidAt,
    int DaysUntilDue,
    BillStatus Status,
    IReadOnlyList<BillPaymentItemDto> Payments
);