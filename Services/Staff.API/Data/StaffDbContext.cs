using Microsoft.EntityFrameworkCore;
using SchoolERP.Staff.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Staff.Data;

public class StaffDbContext : DbContext
{
    public StaffDbContext(DbContextOptions<StaffDbContext> options) : base(options) { }

    public DbSet<StaffProfile> Staff => Set<StaffProfile>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<StaffAttendanceRecord> StaffAttendance => Set<StaffAttendanceRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("staff");

        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.HasIndex(s => s.EmployeeCode).IsUnique();
            entity.Property(s => s.EmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(s => s.FullName).HasMaxLength(200).IsRequired();
            entity.Property(s => s.MonthlySalary).HasPrecision(12, 2);
            entity.HasQueryFilter(s => !s.IsDeleted);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasIndex(l => new { l.StaffId, l.FromDateUtc });
            entity.HasOne(l => l.Staff)
                  .WithMany()
                  .HasForeignKey(l => l.StaffId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payout>(entity =>
        {
            entity.HasIndex(p => new { p.StaffId, p.PaidOnUtc });
            entity.Property(p => p.PeriodLabel).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Amount).HasPrecision(12, 2);
            entity.HasOne(p => p.Staff)
                  .WithMany()
                  .HasForeignKey(p => p.StaffId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StaffAttendanceRecord>(entity =>
        {
            entity.HasIndex(a => new { a.StaffId, a.Date }).IsUnique();
            entity.Property(a => a.Status).HasMaxLength(20).IsRequired();
            entity.HasOne(a => a.Staff)
                  .WithMany()
                  .HasForeignKey(a => a.StaffId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });
    }
}
