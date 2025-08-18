using BudgetForge.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetForge.Infrastructure.Data.Configurations;

public class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
{
    public void Configure(EntityTypeBuilder<BillPayment> b)
    {
        b.ToTable("BillPayments");
        b.HasKey(x => x.Id);

        b.Property(x => x.BillId).IsRequired();

        b.Property(x => x.PaidAt).IsRequired();

        b.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(x => x.Notes)
            .HasMaxLength(240);

        // Match the parent's filter so EF is happy
        b.HasQueryFilter(p => p.Bill.IsActive);
    }
}