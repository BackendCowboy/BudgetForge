using BudgetForge.Application.DTOs;
using BudgetForge.Application.Interfaces;
using BudgetForge.Infrastructure.Data;
using BudgetForge.Domain.Entities;           // <-- brings in TransactionType
using Microsoft.EntityFrameworkCore;

namespace BudgetForge.Infrastructure.Services
{
    public class BudgetSummaryService : IBudgetSummaryService
    {
        private readonly ApplicationDbContext _db;

        public BudgetSummaryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<BudgetSummaryDto> GetSummaryAsync(
            DateTime from, DateTime to, int? accountId, CancellationToken ct)
        {
            var q = _db.Transactions
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.Date >= from && t.Date <= to);

            if (accountId.HasValue)
                q = q.Where(t => t.AccountId == accountId.Value);

            // Sum by enum type (Amount is positive for both income & expense)
            var monthly = await q
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new MonthlyTotal
                {
                    Year    = g.Key.Year,
                    Month   = g.Key.Month,
                    Income  = g.Sum(e => e.Amount > 0 ? (decimal?)e.Amount : 0) ?? 0m,
                    Expense = g.Sum(e => e.Amount < 0 ? (decimal?)(-e.Amount) : 0) ?? 0m
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToListAsync(ct);

            var totalIncome   = monthly.Sum(m => m.Income);
            var totalExpenses = monthly.Sum(m => m.Expense);

            return new BudgetSummaryDto
            {
                From          = from,
                To            = to,
                TotalIncome   = totalIncome,
                TotalExpenses = totalExpenses,
                Monthly       = monthly,
                TopCategories = new List<CategoryTotal>()
            };
        }
    }
}