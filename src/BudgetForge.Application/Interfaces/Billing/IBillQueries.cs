using BudgetForge.Application.DTOs.Billing;

namespace BudgetForge.Application.Interfaces.Billing;

public interface IBillQueries
{
    Task<IReadOnlyList<BillListItemDto>> GetUpcomingAsync(int userId, int horizonDays = 30, CancellationToken ct = default);

    Task<BillDetailsDto?> GetByIdAsync(int userId, Guid billId, CancellationToken ct = default);
}