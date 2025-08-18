using System.Linq;
using BudgetForge.Domain.Entities.Billing;
using BudgetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BudgetForge.Infrastructure.Tests;

public class BillingQueryFilterTests
{
    [Fact]
    public void Bills_GlobalQueryFilter_Excludes_Inactive()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("BillingFilterDb")
            .Options;

        using var db = new ApplicationDbContext(options);

        db.Bills.Add(new Bill
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            Name = "Active Bill",
            Amount = 100m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            IsRecurring = true,
            Frequency = Domain.Enums.Billing.Frequency.Monthly,
            IsActive = true
        });

        db.Bills.Add(new Bill
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            Name = "Inactive Bill",
            Amount = 50m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            IsRecurring = false,
            IsActive = false
        });

        db.SaveChanges();

        // Default set should apply HasQueryFilter and hide inactive rows
        var visible = db.Bills.AsNoTracking().ToList();

        Assert.Single(visible);
        Assert.Equal("Active Bill", visible[0].Name);

        // Bypass filter to confirm both exist
        var all = db.Bills.IgnoreQueryFilters().AsNoTracking().ToList();
        Assert.Equal(2, all.Count);
    }
}