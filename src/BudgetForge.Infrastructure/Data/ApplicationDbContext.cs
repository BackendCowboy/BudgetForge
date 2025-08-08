using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using BudgetForge.Domain.Entities;
using BudgetForge.Infrastructure.Identity; // <-- AppUser lives here

namespace BudgetForge.Infrastructure.Data
{
    // We now inherit from IdentityDbContext to use ASP.NET Core Identity for users/roles.
    // Identity creates: AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
    // Domain tables (Accounts, Transactions, RefreshTokens) remain and now FK to AppUser.
    public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Identity owns users/roles; only domain sets here:
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------------------------------------------------
            // RefreshToken
            // ------------------------------------------------------------
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.RevokedAt).IsRequired(false);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
                entity.Property(e => e.RevokedByIp).HasMaxLength(45);
                entity.Property(e => e.CreatedByIp).HasMaxLength(45);

                // Computed (not mapped)
                entity.Ignore(e => e.IsExpired);
                entity.Ignore(e => e.IsActive);

                entity.HasIndex(e => e.Token).IsUnique();

                // FK -> AppUser (no dependent-side nav in Domain)
                entity.HasOne<AppUser>()
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------------------
            // Account
            // ------------------------------------------------------------
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Balance).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("CAD");
                entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasIndex(e => new { e.UserId, e.IsDeleted });

                // FK -> AppUser (no dependent-side nav in Domain)
                entity.HasOne<AppUser>()
                      .WithMany(u => u.Accounts)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Account -> Transactions
                entity.HasMany(a => a.Transactions!)
                      .WithOne(t => t.Account!)
                      .HasForeignKey(t => t.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------------------
            // Transaction
            // ------------------------------------------------------------
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasIndex(e => new { e.AccountId, e.Date });
                entity.HasIndex(e => new { e.AccountId, e.IsDeleted });
            });

            // No EF seeding for roles here. Create "Admin"/"User" at startup via RoleManager.
        }
    }
}