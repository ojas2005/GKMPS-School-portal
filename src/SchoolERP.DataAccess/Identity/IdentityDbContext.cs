using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Identity;

/// <summary>
/// Owns the "identity" database on the shared TiDB server. Other modules never query these
/// tables directly -- they go through the Identity module's business services or events.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
            entity.Property(u => u.TwoFactorSecret).HasMaxLength(256);
            entity.Property(u => u.TwoFactorRecoveryCodes).HasMaxLength(1024);
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
            entity.HasIndex(rt => rt.SessionId);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => new { s.UserId, s.EndedAtUtc });
            entity.Property(s => s.EndReason).HasMaxLength(32);
            entity.Property(s => s.CreatedByIp).HasMaxLength(64);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
            entity.HasIndex(a => a.TimestampUtc);
        });


        // Pomelo defaults Guid columns to collation "ascii_general_ci", which TiDB's new
        // collation framework doesn't support (only ascii_bin is in its allowed set).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                {
                    property.SetCollation("ascii_bin");
                }
            }
        }
    }
}
