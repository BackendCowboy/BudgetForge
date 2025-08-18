using BudgetForge.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetForge.Infrastructure.Data.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> b)
    {
        b.ToTable("Bills");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120);

        b.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Npgsql EF Core 8 supports DateOnly natively
        b.Property(x => x.DueDate).IsRequired();

        b.Property(x => x.IsRecurring).IsRequired();

        b.Property(x => x.Frequency)
            .HasConversion<int?>();

        b.Property(x => x.Category)
            .HasMaxLength(60);

        b.Property(x => x.AutoPay).IsRequired();

        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).IsRequired();

        b.Property(x => x.LastPaidAt);

        b.HasMany(x => x.Payments)
            .WithOne(p => p.Bill)
            .HasForeignKey(p => p.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete filter
        b.HasQueryFilter(x => x.IsActive);
    }
}