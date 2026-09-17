using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Attendance.Entities;
using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Attendance;

public class AttendanceDbContext : DbContext
{
    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options) { }

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<MonthlyAttendanceSummary> MonthlySummaries => Set<MonthlyAttendanceSummary>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            // Data-integrity guarantee: exactly one attendance record per student per day.
            entity.HasIndex(a => new { a.StudentId, a.Date }).IsUnique();
            // Hot lookup path: "everyone marked in a class/section on a given day".
            entity.HasIndex(a => new { a.ClassId, a.SectionId, a.Date });
            entity.Property(a => a.Date).HasColumnType("date");
            entity.Property(a => a.ArrivalTime).HasColumnType("time");
        });

        modelBuilder.Entity<MonthlyAttendanceSummary>(entity =>
        {
            entity.HasIndex(m => new { m.StudentId, m.Year, m.Month }).IsUnique();
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
