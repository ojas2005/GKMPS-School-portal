using Microsoft.EntityFrameworkCore;
using SchoolERP.Attendance.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Attendance.Data;

public class AttendanceDbContext : DbContext
{
    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options) { }

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<MonthlyAttendanceSummary> MonthlySummaries => Set<MonthlyAttendanceSummary>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("attendance");

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
    }
}
