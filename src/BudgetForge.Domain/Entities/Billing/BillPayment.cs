namespace BudgetForge.Domain.Entities.Billing;

public class BillPayment
{
    public Guid Id { get; set; }

    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }          // decimal(18,2) in DB
    public string? Notes { get; set; }
}