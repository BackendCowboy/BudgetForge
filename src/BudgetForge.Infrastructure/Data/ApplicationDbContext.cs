using Microsoft.EntityFrameworkCore;
using BudgetForge.Domain.Entities;

namespace BudgetForge.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets represent tables in your database
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Configure properties
                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                // Create unique index on email
                entity.HasIndex(e => e.Email)
                    .IsUnique();

                // Configure timestamps
                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .IsRequired(false);

                entity.Property(e => e.LastLoginAt)
                    .IsRequired(false);

                // Configure computed property (not stored in DB)
                entity.Ignore(e => e.FullName);
            });

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(200);

                // Create unique index on role name
                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();
            });

            // Configure UserRole entity (many-to-many junction table)
            modelBuilder.Entity<UserRole>(entity =>
            {
                // Composite primary key
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.Property(e => e.AssignedAt)
                    .IsRequired();

                entity.Property(e => e.AssignedBy)
                    .HasMaxLength(100);

                // Configure relationships
                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RefreshToken entity
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
                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Administrator with full access", CreatedAt = DateTime.UtcNow },
                new Role { Id = 2, Name = "User", Description = "Standard user with basic access", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}