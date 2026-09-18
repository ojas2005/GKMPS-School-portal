using Microsoft.EntityFrameworkCore;

namespace SchoolERP.DataAccess.Audit;

/// <summary>The durable audit trail, in its own "audit" database on the shared server.</summary>
public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditEntry> Entries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntries");
            entity.Property(e => e.Action).HasMaxLength(150).IsRequired();
            entity.Property(e => e.ActorRole).HasMaxLength(32);
            entity.Property(e => e.EntityId).HasMaxLength(64);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.Detail).HasMaxLength(500);
            entity.Property(e => e.ActorUserId).UseCollation("ascii_bin");
            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => new { e.ActorUserId, e.TimestampUtc });
            entity.HasIndex(e => new { e.EntityId, e.TimestampUtc });
        });
    }
}
