using BudgetForge.Application.DTOs.Billing;

namespace BudgetForge.Application.Interfaces.Billing;

public interface IBillCommands
{
    Task<Guid> CreateAsync(int userId, BillCreateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Records a payment for a bill. If the bill is recurring, advances its DueDate.
    /// Returns the created BillPayment Id.
    /// </summary>
    Task<Guid> PayAsync(int userId, Guid billId, BillPaymentCreateDto dto, CancellationToken ct = default);
}