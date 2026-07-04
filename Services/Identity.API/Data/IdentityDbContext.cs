using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Identity.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Identity.Data;

/// <summary>
/// Owns the "identity" PostgreSQL schema exclusively. No other microservice is permitted
/// to query these tables directly -- everything crosses the boundary via Identity.API's
/// HTTP endpoints or the events it publishes.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(100);
            entity.HasIndex(u => u.GoogleSubjectId).IsUnique(false);
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
            entity.HasQueryFilter(u => !u.IsDeleted);

            entity.HasMany(u => u.RefreshTokens)
                  .WithOne(rt => rt.User)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => new { rt.UserId, rt.ExpiresAtUtc });
            entity.Property(rt => rt.TokenHash).HasMaxLength(512).IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
            entity.HasIndex(a => a.TimestampUtc);
        });

        // Required tables for MassTransit's EF Core Outbox (AddEntityFrameworkOutbox in
        // Program.cs) so events publish reliably alongside the transaction that created them.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
