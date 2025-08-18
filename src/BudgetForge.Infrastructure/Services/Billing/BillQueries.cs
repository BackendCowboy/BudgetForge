using BudgetForge.Application.DTOs.Billing;
using BudgetForge.Application.Interfaces.Billing;
using BudgetForge.Domain.Enums.Billing;
using BudgetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetForge.Infrastructure.Services.Billing;

public class BillQueries : IBillQueries
{
    private readonly ApplicationDbContext _db;

    public BillQueries(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<BillListItemDto>> GetUpcomingAsync(
        int userId, int horizonDays = 30, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(horizonDays);

        var rows = await _db.Bills.AsNoTracking()
            .Where(b => b.UserId == userId
                        && b.IsActive
                        && b.DueDate >= today
                        && b.DueDate <= horizon)
            .OrderBy(b => b.DueDate)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Amount,
                b.DueDate,
                b.IsRecurring,
                b.Frequency,
                b.Category,
                b.AutoPay
            })
            .ToListAsync(ct);

        var list = new List<BillListItemDto>(rows.Count);

        foreach (var r in rows)
        {
            var days = r.DueDate.DayNumber - today.DayNumber;
            var status = days < 0
                ? BillStatus.Overdue
                : days <= 7 ? BillStatus.DueSoon
                : BillStatus.Pending;

            list.Add(new BillListItemDto(
                r.Id, r.Name, r.Amount, r.DueDate,
                r.IsRecurring, r.Frequency, r.Category, r.AutoPay,
                days, status));
        }

        return list;
    }

    // NEW: bill details + payment history
    public async Task<BillDetailsDto?> GetByIdAsync(
        int userId, Guid billId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var bill = await _db.Bills
            .AsNoTracking()
            .Where(b => b.Id == billId && b.UserId == userId)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Amount,
                b.DueDate,
                b.IsRecurring,
                b.Frequency,
                b.Category,
                b.AutoPay,
                b.IsActive,
                b.LastPaidAt
            })
            .FirstOrDefaultAsync(ct);

        if (bill is null) return null;

        var days = bill.DueDate.DayNumber - today.DayNumber;
        var status = days < 0
            ? BillStatus.Overdue
            : days <= 7 ? BillStatus.DueSoon
            : BillStatus.Pending;

        var payments = await _db.BillPayments
            .AsNoTracking()
            .Where(p => p.BillId == bill.Id)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new BillPaymentItemDto(p.Id, p.Amount, p.PaidAt, p.Notes))
            .ToListAsync(ct);

        return new BillDetailsDto(
            bill.Id,
            bill.Name,
            bill.Amount,
            bill.DueDate,
            bill.IsRecurring,
            bill.Frequency,
            bill.Category,
            bill.AutoPay,
            bill.IsActive,
            bill.LastPaidAt,
            days,
            status,
            payments
        );
    }
}