using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Fee.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Fee.Data;

public class FeeDbContext : DbContext
{
    public FeeDbContext(DbContextOptions<FeeDbContext> options) : base(options) { }

    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<FeeStructure>(entity =>
        {
            entity.HasIndex(f => new { f.ClassId, f.AcademicYear });
        });

        modelBuilder.Entity<FeePayment>(entity =>
        {
            // MySQL/TiDB treats NULLs as distinct in a unique index, so multiple ad-hoc
            // (FeeStructureId == null) rows per student are still allowed here -- the
            // "only one opening balance per student" rule is enforced in the service layer.
            entity.HasIndex(p => new { p.StudentId, p.FeeStructureId }).IsUnique();
            entity.HasIndex(p => p.ClassId); // backs the owner's pending-by-class summary
            entity.Property(p => p.PeriodLabel).HasMaxLength(50);
            entity.Ignore(p => p.Status); // computed, not persisted
            entity.HasOne(p => p.FeeStructure)
                  .WithMany()
                  .HasForeignKey(p => p.FeeStructureId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(t => t.ReceiptNumber).IsUnique();
            entity.HasIndex(t => t.FeePaymentId);
            entity.Property(t => t.ReceiptNumber).HasMaxLength(64).IsRequired();
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
