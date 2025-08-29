using BudgetForge.Application.DTOs;

namespace BudgetForge.Application.Interfaces;

public interface IBudgetSummaryService
{
    Task<BudgetSummaryDto> GetSummaryAsync(
        DateTime from,
        DateTime to,
        int? accountId = null,
        CancellationToken ct = default);
}