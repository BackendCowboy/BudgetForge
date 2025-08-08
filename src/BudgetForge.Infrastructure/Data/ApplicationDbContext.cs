using Microsoft.EntityFrameworkCore;
using BudgetForge.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BudgetForge.Infrastructure.Data
{
    // NOTE:
    // We now inherit from IdentityDbContext to use ASP.NET Core Identity for users/roles.
    // This replaces the legacy custom Users/Roles/UserRoles tables & mappings.
    // Identity will create: AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
    // Your domain tables (Accounts, Transactions, RefreshTokens) remain and now FK to AppUser.
    public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets represent tables in your database
        // REMOVED: Users, Roles, UserRoles — handled by Identity
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------------------------------------------------
            // Configure RefreshToken entity
            // ------------------------------------------------------------
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.ExpiresAt)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.RevokedAt)
                    .IsRequired(false);

                entity.Property(e => e.ReplacedByToken)
                    .HasMaxLength(500);

                entity.Property(e => e.RevokedByIp)
                    .HasMaxLength(45); // IPv6 max length

                entity.Property(e => e.CreatedByIp)
                    .HasMaxLength(45);

                // Configure computed properties (not stored in DB)
                entity.Ignore(e => e.IsExpired);
                entity.Ignore(e => e.IsActive);

                // Create index on token for fast lookups
                entity.HasIndex(e => e.Token)
                    .IsUnique();

                // Configure relationship
                // CHANGED: FK now points to Identity AppUser instead of legacy User
                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------------------
            // Configure Account entity
            // ------------------------------------------------------------
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Balance)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .HasDefaultValue("CAD");

                // Store enum as string for readability
                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .IsRequired();

                // Create indexes for performance
                entity.HasIndex(e => new { e.UserId, e.IsDeleted });

                // Configure relationship with User
                // CHANGED: navigation targets AppUser (Identity) instead of legacy User
                entity.HasOne(a => a.User)
                    .WithMany(u => u.Accounts)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship with Transactions
                entity.HasMany(a => a.Transactions)
                    .WithOne(t => t.Account)
                    .HasForeignKey(t => t.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------------------
            // Configure Transaction entity
            // ------------------------------------------------------------
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(200);

                // Store enum as string for readability
                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.Date)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .IsRequired();

                // Create indexes for performance
                entity.HasIndex(e => new { e.AccountId, e.Date });
                entity.HasIndex(e => new { e.AccountId, e.IsDeleted });

                // Relationship is configured from Account side
            });

            // ------------------------------------------------------------
            // Seed initial roles
            // ------------------------------------------------------------
            // NOTE: Role seeding is no longer done here via EF for the custom Role entity.
            // Use RoleManager<IdentityRole<int>> at startup to ensure "Admin" and "User" exist:
            //
            // using (var scope = app.Services.CreateScope())
            // {
            //     var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            //     foreach (var r in new[] { "Admin", "User" })
            //         if (!await roles.RoleExistsAsync(r))
            //             await roles.CreateAsync(new IdentityRole<int>(r));
            // }
        }
    }
}