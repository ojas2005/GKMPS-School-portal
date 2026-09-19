using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Academic;

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
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasIndex(s => new { s.ClassId, s.Code }).IsUnique();
        });

        modelBuilder.Entity<Timetable>(entity =>
        {
            entity.HasIndex(t => new { t.ClassId, t.SectionId }).IsUnique();
            entity.Property(t => t.SlotsJson).HasColumnType("json");
        });

        modelBuilder.Entity<ScheduleConfig>(entity =>
        {
            entity.Property(c => c.ConfigJson).HasColumnType("json");
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
