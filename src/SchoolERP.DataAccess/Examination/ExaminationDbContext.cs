using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Examination.Entities;
using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Examination;

public class ExaminationDbContext : DbContext
{
    public ExaminationDbContext(DbContextOptions<ExaminationDbContext> options) : base(options) { }

    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<MarksEntry> MarksEntries => Set<MarksEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasIndex(e => new { e.ClassId, e.SubjectId, e.ExamDateUtc });
            entity.Property(e => e.AnswerKeyJson).HasColumnType("json");
        });

        modelBuilder.Entity<MarksEntry>(entity =>
        {
            // Data-integrity guarantee: one marks entry per student per exam.
            entity.HasIndex(m => new { m.ExamId, m.StudentId }).IsUnique();
            entity.HasOne(m => m.Exam)
                  .WithMany()
                  .HasForeignKey(m => m.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);
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
