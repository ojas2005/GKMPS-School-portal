using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Reporting.Entities;
using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Reporting;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<ReportSnapshot> ReportSnapshots => Set<ReportSnapshot>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<ReportSnapshot>(entity =>
        {
            entity.HasIndex(r => new { r.ReportType, r.CreatedAtUtc });
            entity.Property(r => r.ResultJson).HasColumnType("json");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
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
