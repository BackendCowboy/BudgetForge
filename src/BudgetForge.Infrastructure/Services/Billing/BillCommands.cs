using BudgetForge.Application.DTOs.Billing;
using BudgetForge.Application.Interfaces.Billing;
using BudgetForge.Domain.Entities.Billing;
using BudgetForge.Domain.Enums.Billing;
using BudgetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetForge.Infrastructure.Services.Billing;

public class BillCommands : IBillCommands
{
    private readonly ApplicationDbContext _db;

    public BillCommands(ApplicationDbContext db) => _db = db;

    public async Task<Guid> CreateAsync(int userId, BillCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required");
        if (dto.Amount <= 0)
            throw new ArgumentException("Amount must be positive");
        if (dto.IsRecurring && dto.Frequency is null)
            throw new ArgumentException("Frequency is required for recurring bills");

        var exists = await _db.Bills.AnyAsync(b =>
            b.UserId == userId && b.IsActive && b.Name == dto.Name && b.DueDate == dto.DueDate, ct);
        if (exists) throw new InvalidOperationException("A bill with the same name and due date already exists");

        var bill = new Bill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Amount = dto.Amount,
            DueDate = dto.DueDate,
            IsRecurring = dto.IsRecurring,
            Frequency = dto.Frequency,
            Category = dto.Category?.Trim(),
            AutoPay = dto.AutoPay,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct);
        return bill.Id;
    }

    public async Task<Guid> PayAsync(int userId, Guid billId, BillPaymentCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0) throw new ArgumentException("Payment amount must be positive");

        var bill = await _db.Bills
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == billId && b.UserId == userId && b.IsActive, ct);

        if (bill is null) throw new KeyNotFoundException("Bill not found");

        var paidAt = dto.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow;

        var payment = new BillPayment
        {
            Id = Guid.NewGuid(),
            BillId = bill.Id,
            Amount = dto.Amount,
            PaidAt = paidAt,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes!.Trim()
        };

        _db.BillPayments.Add(payment);

        // mark last paid
        bill.LastPaidAt = paidAt;

        // simple rule: if not recurring, deactivate the bill after payment
        if (!bill.IsRecurring)
        {
            bill.IsActive = false;
        }
        else
        {
            // advance next due date based on frequency
            bill.DueDate = Advance(bill.DueDate, bill.Frequency ?? Frequency.Monthly);
        }

        await _db.SaveChangesAsync(ct);
        return payment.Id;
    }

    private static DateOnly Advance(DateOnly due, Frequency freq)
    {
        return freq switch
        {
            Frequency.Weekly    => due.AddDays(7),
            Frequency.BiWeekly  => due.AddDays(14),
            Frequency.Monthly   => AddMonthsSafe(due, 1),
            Frequency.Quarterly => AddMonthsSafe(due, 3),
            Frequency.Yearly    => AddYearsSafe(due, 1),
            _                   => due // Custom: no-op here; you can extend later
        };
    }

    private static DateOnly AddMonthsSafe(DateOnly date, int months)
    {
        var targetMonth = date.AddMonths(months);
        // If original day was 29/30/31 and target month is shorter, clamp to month end
        var daysInTarget = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);
        var day = Math.Min(date.Day, daysInTarget);
        return new DateOnly(targetMonth.Year, targetMonth.Month, day);
    }

    private static DateOnly AddYearsSafe(DateOnly date, int years)
    {
        var target = date.AddYears(years);
        var daysInTarget = DateTime.DaysInMonth(target.Year, target.Month);
        var day = Math.Min(date.Day, daysInTarget);
        return new DateOnly(target.Year, target.Month, day);
    }
}