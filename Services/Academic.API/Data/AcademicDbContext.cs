using Microsoft.EntityFrameworkCore;
using SchoolERP.Academic.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Academic.Data;

public class AcademicDbContext : DbContext
{
    public AcademicDbContext(DbContextOptions<AcademicDbContext> options) : base(options) { }

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Timetable> Timetables => Set<Timetable>();
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<ScheduleConfig> ScheduleConfigs => Set<ScheduleConfig>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("academic");

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasIndex(s => new { s.ClassId, s.Code }).IsUnique();
        });

        modelBuilder.Entity<Timetable>(entity =>
        {
            entity.HasIndex(t => new { t.ClassId, t.SectionId }).IsUnique();
            entity.Property(t => t.SlotsJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<ScheduleConfig>(entity =>
        {
            entity.Property(c => c.ConfigJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Homework>(entity =>
        {
            entity.HasIndex(h => new { h.ClassId, h.SectionId, h.DueDateUtc });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });
    }
}
