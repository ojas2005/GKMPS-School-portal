using Microsoft.EntityFrameworkCore;
using SchoolERP.Reporting.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Reporting.Data;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<ReportSnapshot> ReportSnapshots => Set<ReportSnapshot>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");

        modelBuilder.Entity<ReportSnapshot>(entity =>
        {
            entity.HasIndex(r => new { r.ReportType, r.CreatedAtUtc });
            entity.Property(r => r.ResultJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });
    }
}
