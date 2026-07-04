using Microsoft.EntityFrameworkCore;
using SchoolERP.Examination.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Examination.Data;

public class ExaminationDbContext : DbContext
{
    public ExaminationDbContext(DbContextOptions<ExaminationDbContext> options) : base(options) { }

    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<MarksEntry> MarksEntries => Set<MarksEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("examination");

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasIndex(e => new { e.ClassId, e.SubjectId, e.ExamDateUtc });
            entity.Property(e => e.AnswerKeyJson).HasColumnType("jsonb");
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
    }
}
