using BudgetForge.Domain.Enums.Billing;

namespace BudgetForge.Domain.Entities.Billing;

public class Bill
{
    public Guid Id { get; set; }
    public int UserId { get; set; }   // align with Identity int key

    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }

    public bool IsRecurring { get; set; }
    public Frequency? Frequency { get; set; }

    public string? Category { get; set; }
    public bool AutoPay { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastPaidAt { get; set; }

    public ICollection<BillPayment> Payments { get; set; } = new List<BillPayment>();
}