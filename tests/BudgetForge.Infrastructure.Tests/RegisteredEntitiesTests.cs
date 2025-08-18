using System.Linq;
using BudgetForge.Domain.Entities.Billing;
using BudgetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BudgetForge.Infrastructure.Tests;

public class RegisteredEntitiesTests
{
    [Fact]
    public void Bills_AreRegisteredInModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("ModelCheckDb")
            .Options;

        using var db = new ApplicationDbContext(options);

        var entityTypes = db.Model.GetEntityTypes().Select(e => e.ClrType).ToList();

        Assert.Contains(typeof(Bill), entityTypes);
        Assert.Contains(typeof(BillPayment), entityTypes);
    }
}