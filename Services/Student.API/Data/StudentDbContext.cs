using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Student.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Student.Data;

public class StudentDbContext : DbContext
{
    public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options) { }

    public DbSet<StudentProfile> Students => Set<StudentProfile>();
    public DbSet<StudentDocument> Documents => Set<StudentDocument>();
    public DbSet<TransferCertificate> TransferCertificates => Set<TransferCertificate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("student");

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(s => s.AdmissionNumber).IsUnique();
            // Composite index on the hot lookup path: "all active students in a class/section".
            entity.HasIndex(s => new { s.ClassId, s.SectionId });
            entity.Property(s => s.AdmissionNumber).HasMaxLength(50).IsRequired();
            entity.Property(s => s.FullName).HasMaxLength(200).IsRequired();
            entity.HasQueryFilter(s => !s.IsDeleted);
        });

        modelBuilder.Entity<StudentDocument>(entity =>
        {
            entity.HasIndex(d => new { d.StudentId, d.DocumentType });
            entity.HasOne(d => d.Student)
                  .WithMany()
                  .HasForeignKey(d => d.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TransferCertificate>(entity =>
        {
            entity.HasIndex(tc => tc.VerificationCode).IsUnique();
            entity.HasIndex(tc => tc.StudentId);
            entity.Property(tc => tc.VerificationCode).HasMaxLength(64).IsRequired();
            entity.HasOne(tc => tc.Student)
                  .WithMany()
                  .HasForeignKey(tc => tc.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });

        // Required tables for MassTransit's EF Core Outbox (AddEntityFrameworkOutbox in Program.cs).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
